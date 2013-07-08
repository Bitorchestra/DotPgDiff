using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace BO.DotPgDiff
{
	public partial class Main : Form
	{
		private Model.Database _sourceModel;
		private Model.Database _targetModel;

		public Main()
		{
			InitializeComponent();
		}

		private Npgsql.NpgsqlConnectionStringBuilder GetConnectionString()
		{
			using (var c = new Connect()) {
				if (c.ShowDialog(this) == DialogResult.OK) {
					return c.ConnectionString;
				} else {
					return null;
				}
			}
		}

		#region Events
		private void sourceConnect_Click(object sender, EventArgs e)
		{
			this.LoadTree(sourceTree, sourceDescription, ref _sourceModel);
		}
		private void targetConnect_Click(object sender, EventArgs e)
		{
			this.LoadTree(targetTree, targetDescription, ref _targetModel);
		}
		private void LoadTree(TreeView t, Label l, ref Model.Database target)
		{
			var model = this.LoadTree(t, l);
			if (model != null)
				target = model;
			btnDiff.Enabled = (_sourceModel != null && _targetModel != null);
		}

		private Model.Base GetElement(Model.Database db, TreeNode n)
		{
			try {
				return db.Elements[n.Tag.ToString()][long.Parse(n.Name)];
			} catch (Exception e) {
				return null;
			}
		}

		private string GetElementSource(Model.Base elm)
		{
			return "-- " + String.Join("\r\n\r\n-- ", elm.SqlDrop.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
				+ "\r\n\r\n" + elm.SqlCreate.Replace("\n", "\r\n");
		}

		private void sourceTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try {
				var elm = this.GetElement(_sourceModel, e.Node);
				SQLSource.Text = "-- SOURCE\r\n" + GetElementSource(elm);
			} catch { }
		}
		private void targetTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try {
				var elm = this.GetElement(_targetModel, e.Node);
				SQLTarget.Text = "-- TARGET\r\n" + GetElementSource(elm);
			} catch { }
		}
		private void Main_SizeChanged(object sender, EventArgs e)
		{
			var w = splitContainer2.Panel1.Width / 2;
			SQLSource.Width = SQLTarget.Width = w;
			SQLTarget.Left = SQLSource.Right;
		}

		private void btnCopyTarget_Click(object sender, EventArgs e)
		{
			Clipboard.Clear();
			Clipboard.SetText(SQLTarget.Text);
		}

		private void btnCopySource_Click(object sender, EventArgs e)
		{
			Clipboard.Clear();
			Clipboard.SetText(SQLSource.Text);
		}
		#endregion

		#region BuildNode
		private TreeNode BuildNode(Model.IBase element)
		{
			if (element is Model.IScoped) {
				return new TreeNode {
					Name = element.Oid.ToString(),
					Text = string.Format("{0}.{1}", (element as Model.IScoped).ParentName, element.DisplayName),
					Tag = element.TypeName,
					BackColor = Color.White,
					ForeColor = Color.Black
				};
			} else {
				return new TreeNode {
					Name = element.Oid.ToString(),
					Text = element.DisplayName,
					Tag = element.TypeName,
					BackColor = Color.White,
					ForeColor = Color.Black
				};
			}
		}
		private TreeNode BuildNode(Model.Base element)
		{
			var rt = new TreeNode {
				Name = element.Oid.ToString(),
				Text = element.DisplayName,
				Tag = element.TypeName,
				BackColor = Color.White,
				ForeColor = Color.Black
			};
			return rt;
		}
		// groups
		private TreeNode BuildNode(string name, int? count = null, string tag = null)
		{
			return new TreeNode {
				Text = (count.HasValue ? string.Format("{0} ({1})", name, count) : name),
				Tag = tag,
				BackColor = Color.Green,
				ForeColor = Color.White
			};
		}

		private void BuildGroup<T>(string name, List<T> elements, TreeNode parent)
			where T : Model.Base
		{
			if (elements.Count > 0) {
				var tGroup = this.BuildNode(name/*, elements.Count*/);
				foreach (var t in elements) {
					var tNode = this.BuildNode(t);
					/*
                    if (t is Model.CompositeType) {
                        this.BuildGroup("Columns", (t as Model.CompositeType).Columns, tNode);
                    }
					 */

					tGroup.Nodes.Add(tNode);
				}
				parent.Nodes.Add(tGroup);
			}
		}
		#endregion

		#region Tree
		private Model.Database LoadTree(TreeView tv, Label desc)
		{
			try {
				var cs = this.GetConnectionString();
				if (cs == null) {
					return null;
				}

				desc.Text = string.Format("{0}@{1}:{2}:{3}", cs.UserName, cs.Host, cs.Port, cs.Database);
				tv.Nodes.Clear();

				using (var c = new Query.Loader(cs)) {
					var rt = c.Load();

					foreach (var s in rt.Schemas) {
						// add schema
						var sNode = this.BuildNode(s);
						// add schema types
						this.BuildGroup("Types", s.CompositeTypes, sNode);
						// add schema tables
						this.BuildGroup("Tables", s.Tables, sNode);
						// add schema views
						this.BuildGroup("Views", s.Views, sNode);
						// add schema functions
						this.BuildGroup("Functions", s.Functions, sNode);
						// add schema operators
						this.BuildGroup("Operators", s.Operators, sNode);

						tv.Nodes.Add(sNode);
					}

					return rt;
				}
			} catch (Exception e) {
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK);
				return null;
			}
		}
		#endregion

		#region Differences
		private static readonly Regex rxSpace = new Regex("\\s+");

		private bool DecorateDifferences(TreeNodeCollection source)
		{
			bool rt = false;
			for (int i = 0; i < source.Count; ++i) {
				var n = source[i];
				rt = this.DecorateDifferences(n) || rt;
			}
			return rt;
		}
		private bool DecorateDifferences(TreeNode source)
		{
			bool rt = this.DecorateDifferences(source.Nodes);

			if (source.BackColor == Color.White) {
				source.BackColor = Color.Blue; source.ForeColor = Color.White;
			}

			if (source.BackColor != Color.Green) {
				rt = true;
			} else if (rt) {
				source.BackColor = Color.Red; source.ForeColor = Color.White;
			}

			return rt;
		}

		private void DecorateDifferences(TreeNodeCollection source, TreeNodeCollection target)
		{
			for (int i = 0; i < source.Count; ++i) {
				TreeNode srcNode = source[i];
				TreeNode trgNode = null;
				List<TreeNode> equivNodes = new List<TreeNode>();
				// find equivalent node in target branch
				for (int j = 0; j < target.Count; ++j) {
					var t = target[j];
					if (t.Text == srcNode.Text) {
						// omonimous
						equivNodes.Add(t);
						var srcElm = this.GetElement(_sourceModel, srcNode);
						var trgElm = this.GetElement(_targetModel, t);
						// if same SQL element, or same non-SQL element
						// equivalent is found
						if (srcElm != null && trgElm != null) {
							if (rxSpace.Replace(srcElm.SqlCreate, "") == rxSpace.Replace(trgElm.SqlCreate, "")) {
								trgNode = t;
								break;
							}
						} else if (srcElm == null && trgElm == null) {
							trgNode = t;
							break;
						}
					}
				}
				// if found, decorate green and recurse
				// if not, but has omonimous, decorate them red
				// if not and no omonimous, decorate blue
				if (trgNode != null) {
					srcNode.BackColor = Color.Green; srcNode.ForeColor = Color.White;
					trgNode.BackColor = Color.Green; trgNode.ForeColor = Color.White;
					this.DecorateDifferences(srcNode.Nodes, trgNode.Nodes);
				} else if (equivNodes.Count > 0) {
					srcNode.BackColor = Color.Red; srcNode.ForeColor = Color.White;
					foreach (var n in equivNodes) {
						n.BackColor = Color.Red; n.ForeColor = Color.White;
					}
				} else {
					srcNode.BackColor = Color.Blue; srcNode.ForeColor = Color.White;
				}
			}
		}


		private void btnDiff_Click(object sender, EventArgs e)
		{
			this.DecorateDifferences(sourceTree.Nodes, targetTree.Nodes);
			this.DecorateDifferences(sourceTree.Nodes);
			this.DecorateDifferences(targetTree.Nodes);
		}

		#endregion
		/*
        #region Deps
        private IEnumerable<TreeNode> LoadDepsInNode(Model.IBase elm, IList<Model.IBase> added) {
            List<TreeNode> rt = new List<TreeNode>();
            if (elm.DropCascadesTo.Count > 0) {
                foreach (var d in elm.DropCascadesTo) {
                    if (!added.Contains(d)) {
                        var dNode = this.BuildNode(d);
                        rt.Add(dNode);
                        added.Add(d);
                        dNode.Nodes.AddRange(LoadDepsInNode(d, added).ToArray());
                    }
                }
            }
            return rt;
        }
        #endregion*/
	}
}
