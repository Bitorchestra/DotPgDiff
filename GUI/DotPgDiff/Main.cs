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
		private Npgsql.NpgsqlConnectionStringBuilder _conStrSource = null;
		private Npgsql.NpgsqlConnectionStringBuilder _conStrTarget = null;

		private Dictionary<string, TreeNode> _sourceNodeCache = new Dictionary<string, TreeNode>();
		private Dictionary<string, TreeNode> _targetNodeCache = new Dictionary<string, TreeNode>();

		private Dictionary<string, TreeNode> _sourceNodesToUpdate = new Dictionary<string, TreeNode>();
		private Dictionary<string, TreeNode> _targetNodesToUpdate = new Dictionary<string, TreeNode>();

		public Main()
		{
			InitializeComponent();
		}

		private void Main_SizeChanged(object sender, EventArgs e)
		{
			var w = splitContainer2.Panel1.Width / 2;
			SQLSource.Width = SQLTarget.Width = w;
			SQLTarget.Left = SQLSource.Right;
		}

		#region Connect
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

		private void Connect(ref Npgsql.NpgsqlConnectionStringBuilder connectionString, ref Model.Database model, TreeView tree,
			Label description, Button depLoad, Button copyToClip)
		{
			try {
				connectionString = this.GetConnectionString();
				var m = this.LoadTree(tree, description, connectionString);
				if (m != null) {
					LoadDependenciesBuildTime(connectionString, depLoad);
					depLoad.Enabled = true;
					copyToClip.Enabled = true;
					model = m;
				}
				btnDiff.Enabled = (_sourceModel != null && _targetModel != null);
				btnSync.Enabled = (_sourceModel != null && _targetModel != null);
			} catch (Exception e) {
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void sourceConnect_Click(object sender, EventArgs ex)
		{
			Connect(ref _conStrSource, ref _sourceModel,
				sourceTree, sourceDescription,
				btnDependencyLoadSource, btnCopySource);
		}

		private void targetConnect_Click(object sender, EventArgs ex)
		{
			Connect(ref _conStrTarget, ref _targetModel,
				targetTree, targetDescription,
				btnDependencyLoadTarget, btnCopyTarget);
		}

		#endregion


		#region SQL source view / copy to clipboard

		private void sourceTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			try {
				var elm = this.GetElement(_sourceModel, e.Node);
				SQLSource.Text = "-- SOURCE\r\n" + GetElementSource(elm);
			} catch { }
		}

		private void targetTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			//try {
			var elm = this.GetElement(_targetModel, e.Node);
			SQLTarget.Text = "-- TARGET\r\n" + GetElementSource(elm);
			//} catch { }
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

		#region Dependencies load

		private void LoadDependenciesBuildTime(Npgsql.NpgsqlConnectionStringBuilder _conStr, Button btn)
		{
			try {
				using (var dp = new Query.Update(_conStr)) {
					var lastLoadTime = dp.GetLastLoadTime();
					if (lastLoadTime.HasValue) {
						btn.Text = "Last dependencies build: " + lastLoadTime.ToString();
					} else {
						btn.Text = "Need to build dependencies";
					}
				}
			} catch (Exception ecc) {
				MessageBox.Show(ecc.Message, "Error", MessageBoxButtons.OK);
			}
		}

		private void ReloadDependencies(Npgsql.NpgsqlConnectionStringBuilder _conStr)
		{
			using (var dp = new Query.Update(_conStr))
				dp.RebuildDependencies();
		}


		private void btnDependencyLoadSource_Click(object sender, EventArgs e)
		{
			Cursor = System.Windows.Forms.Cursors.WaitCursor;
			ReloadDependencies(_conStrSource);
			LoadDependenciesBuildTime(_conStrSource, btnDependencyLoadSource);
			var model = this.LoadTree(sourceTree, sourceDescription, _conStrSource);
			if (model != null) {
				_sourceModel = model;
				btnDiff.Enabled = btnSync.Enabled = (_sourceModel != null && _targetModel != null);
			}
			Cursor = System.Windows.Forms.Cursors.Default;
		}

		private void btnDependencyLoadTarget_Click(object sender, EventArgs e)
		{
			Cursor = System.Windows.Forms.Cursors.WaitCursor;
			ReloadDependencies(_conStrTarget);
			LoadDependenciesBuildTime(_conStrTarget, btnDependencyLoadTarget);
			var model = this.LoadTree(targetTree, targetDescription, _conStrTarget);
			if (model != null) {
				_targetModel = model;
				btnDiff.Enabled = btnSync.Enabled = (_sourceModel != null && _targetModel != null);
			}
			Cursor = System.Windows.Forms.Cursors.Default;
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
		private Model.Database LoadTree(TreeView tv, Label desc, Npgsql.NpgsqlConnectionStringBuilder _conStr)
		{
			try {
				if (_conStr == null) {
					return null;
				}

				desc.Text = string.Format("{0}@{1}@{2}", _conStr.UserName, _conStr.Database, _conStr.Host);
				tv.Nodes.Clear();

				using (var c = new Query.Loader(_conStr)) {
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

		private Model.Base GetElement(Model.Database db, TreeNode n)
		{
			try {
				return db.Elements[n.Tag.ToString()][long.Parse(n.Name)];
			} catch (Exception) {
				return null;
			}
		}
		private string GetElementSource(Model.Base elm)
		{
			return "-- " + String.Join("\r\n\r\n-- ", elm.SqlDrop.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
				+ "\r\n\r\n" + elm.SqlCreate.Replace("\n", "\r\n");
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

		private int RedBlueGreen(TreeView tView, TreeNode tNode1)
		{
			foreach (TreeNode tn1 in tView.Nodes) {
				foreach (TreeNode tn2 in tn1.Nodes) {
					foreach (TreeNode tn3 in tn2.Nodes) {
						if (tNode1.Text == tn3.Text) {
							if (tn3.BackColor == Color.Green) {
								return 1;  // green
							} else if (tn3.BackColor == Color.Red) {
								return 2;  // red
							} else {
								return 3;  // blue
							}
						}
					}
				}
			}
			return 3;  // blue
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

						// if having both models, they are SQL-based nodes
						if (srcElm != null && trgElm != null) {
							if (rxSpace.Replace(srcElm.SqlCreate, "") == rxSpace.Replace(trgElm.SqlCreate, "")) {
								trgNode = t;
								break;
							}
						} else if (srcElm == null && trgElm == null) {
							// no model for both: group nodes ("tables", "views", etc)
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

		private void LoadDifferenceCache(TreeView tree, Dictionary<string, TreeNode> dic)
		{
			dic.Clear();
			foreach (TreeNode schemaNode in tree.Nodes) {
				schemaNode.Checked = false;
				dic.Add(schemaNode.Text, schemaNode);
				foreach (TreeNode typeNode in schemaNode.Nodes) {
					typeNode.Checked = false;
					dic.Add(schemaNode.Text + ".." + typeNode.Text, typeNode);
					foreach (TreeNode elementNode in typeNode.Nodes) {
						elementNode.Checked = false;
						dic.Add(elementNode.Text, elementNode);
					}
				}
			}
		}

		private void btnDiff_Click(object sender, EventArgs e)
		{
			this.DecorateDifferences(sourceTree.Nodes, targetTree.Nodes);
			this.DecorateDifferences(sourceTree.Nodes);
			this.DecorateDifferences(targetTree.Nodes);
			this.LoadDifferenceCache(sourceTree, _sourceNodeCache);
			this.LoadDifferenceCache(targetTree, _targetNodeCache);
		}

		#endregion

		#region TreeCheckBox

		private void sourceTree_AfterCheck(object sender, TreeViewEventArgs e)
		{
			CheckNode(sourceTree, sourceTree_AfterCheck, e, _targetNodeCache);
		}
		private void targetTree_AfterCheck(object sender, TreeViewEventArgs e)
		{
			CheckNode(targetTree, targetTree_AfterCheck, e, _sourceNodeCache);
		}

		private void CheckNode(TreeView treeView, TreeViewEventHandler handler, TreeViewEventArgs e, Dictionary<string, TreeNode> otherCache)
		{
			treeView.AfterCheck -= handler;
			// check this
			if (e.Node.BackColor == Color.Green)
				e.Node.Checked = false;
			else
				CheckNodeBranch(e.Node);

			// check correspondent in other tree
			if (e.Node.BackColor == Color.Red) {
				TreeNode tn;
				if (otherCache.TryGetValue(e.Node.Text, out tn)) {
					tn.Checked = e.Node.Checked;
				} else if (e.Node.Parent != null && e.Node.Nodes.Count > 0) {
					if (otherCache.TryGetValue(e.Node.Parent.Text + ".." + e.Node.Text, out tn))
						tn.Checked = e.Node.Checked;
				}
			}

			treeView.AfterCheck += handler;
		}

		private void CheckNodeBranch(TreeNode node)
		{
			// flag everything down, recursively
			CheckNodeChildren(node.Nodes, node.Checked);
			// flag up recursively if branch is fully checked
			CheckNodeParent(node.Parent);
		}

		private void CheckNodeChildren(TreeNodeCollection nodes, bool checkState)
		{
			foreach (TreeNode n in nodes) {
				// check me if my parent is checked and i'm not green
				n.Checked = checkState && n.BackColor != Color.Green;
				CheckNodeChildren(n.Nodes, n.Checked);
			}
		}

		private void CheckNodeParent(TreeNode node)
		{
			if (node == null)
				return;
			// if every checkable child is checked, check parent
			if (node.BackColor == Color.Green) {
				node.Checked = false;
			} else {
				var allChecked = true;
				foreach (TreeNode n in node.Nodes) {
					if (n.BackColor != Color.Green && !n.Checked) {
						allChecked = false;
						break;
					}
				}
				node.Checked = allChecked;
			}

			CheckNodeParent(node.Parent);
		}


		private void RemoveChecks(TreeNodeCollection nodes)
		{
			foreach (TreeNode n in nodes) {
				n.Checked = false;
				RemoveChecks(n.Nodes);
			}
		}

		private void RemoveChecks(TreeView treeView, TreeViewEventHandler checkHandler)
		{
			treeView.AfterCheck -= checkHandler;
			RemoveChecks(treeView.Nodes);
			treeView.AfterCheck += checkHandler;
		}

		#endregion

		#region Syncronize

		private void btnSync_Click(object sender, EventArgs e)
		{
			if (DialogResult.Yes == MessageBox.Show("Proceed with sync ?", "", MessageBoxButtons.YesNo)) {
				Cursor = System.Windows.Forms.Cursors.WaitCursor;

				//percorro l'albero source e inserisco in TDicSource tutti gli elementi il cui CheckBox e segnato

				using (var sourceDependencies = new Query.Update(_conStrSource))
				using (var targetDependencies = new Query.Update(_conStrTarget)) {
		
					LoadElementsToChange(sourceTree, _sourceNodesToUpdate);
					AddParentDependencies(sourceDependencies, _sourceNodeCache, _sourceNodesToUpdate);

					LoadElementsToChange(targetTree, _targetNodesToUpdate);
					AddParentDependencies(targetDependencies, _targetNodeCache, _targetNodesToUpdate);

					List<long> oidsToRemove = new List<long>();
					/////// -	Drop degli elementi (blu, e rossi che dipendono da blu)
					DropTargetElementsDependingOnSchemaToDrop(ref oidsToRemove, targetDependencies);
					/////// -	Drop degli elementi blu restanti in TDicTarget
					DropTargetMissingElements(oidsToRemove, targetDependencies);

					/////// -	Aggiornare gli elementi (rossi) contenuti in TDicSource che non dipendono da un elemento blu
					UpdateDifferentOnes(sourceDependencies, targetDependencies);

					/////// -   Creare gli elementi blu che non dipendono da blu, contenuti in TDicSource
					CreateElementsNotDependingOnSchemaToDrop(sourceDependencies, targetDependencies);

					/////// -	Creare gli elementi blu, e rossi che dipendono almeno un blu, contenuti in TDicSource
					CreateElements(sourceDependencies, targetDependencies);

					/////// -	Eseguire la pr_load in target
					//                MessageBox.Show("pr_load e caricamento\n           su Target");
					//                dpt.prLoad();
					/////// -	Eseguire il LoadTree in modo da aggiornare source e target con i nuovi dati
					//                syncLoadTree();
					/////// -	Eseguire l’evento di colorazione in modo da far vedere le nuove differenze
					//                MessageBox.Show("Diff!");
					//                this.btnDiff_Click(null, null);
				}
				/*******************************   FINE DELL'AGGIORNAMENTO SUL DB   ******************************/
				RemoveChecks(sourceTree, sourceTree_AfterCheck);
				RemoveChecks(targetTree, targetTree_AfterCheck);
				_sourceNodesToUpdate.Clear();
				_targetNodesToUpdate.Clear();
				Cursor = System.Windows.Forms.Cursors.Default;
			} else {
				if (DialogResult.Yes == MessageBox.Show("Remove check marks on source tree ?", "", MessageBoxButtons.YesNo)) {
					RemoveChecks(sourceTree, sourceTree_AfterCheck);
				}
				if (DialogResult.Yes == MessageBox.Show("Remove check marks on target tree ?", "", MessageBoxButtons.YesNo)) {
					RemoveChecks(targetTree, targetTree_AfterCheck);
				}
			}

		}

		private void LoadElementsToChange(TreeView view, Dictionary<string, TreeNode> dic)
		{
			dic.Clear();
			foreach (TreeNode schemaNode in view.Nodes) {
				bool greenElementFound = false;
				foreach (TreeNode typeNode in schemaNode.Nodes)
					foreach (TreeNode elementNode in typeNode.Nodes) {
						if (elementNode.Checked)
							dic.Add(elementNode.Text, elementNode);
						if (elementNode.BackColor == Color.Green)
							greenElementFound = true;
					}
				if (!greenElementFound && schemaNode.BackColor == Color.Blue && schemaNode.Checked)
					dic.Add(schemaNode.Text, schemaNode);
			}
		}

		private void AddParentDependencies(Query.Update dependencies, Dictionary<string, TreeNode> cache, Dictionary<string, TreeNode> nodesToUpdate)
		{
			foreach (KeyValuePair<string, TreeNode> kvps in nodesToUpdate) {
				// per ogni kvp inserisce in prnt tutti i fullname degli elementi dai quali dipende (deps.t_deps)
				var parentNames = dependencies.ListMastersFullnames(kvps.Key);

				// cerca nel albero se alcun genitore è rosso o blu
				// se è rosso o blu verifica se c'è già su TDicSource
				// se non c'è, lo aggiunge
				foreach (string parentName in parentNames) {
					TreeNode parentNode;
					if (cache.TryGetValue(parentName, out parentNode)) {
						if ((parentNode.BackColor == Color.Red || parentNode.BackColor == Color.Blue)
							&& !nodesToUpdate.ContainsKey(parentName))
							nodesToUpdate.Add(parentName, parentNode);
					}
				}
			}
		}

		private void DropTargetElementsDependingOnSchemaToDrop(ref List<long> toRemove, Query.Update targetDependencies)
		{
			foreach (KeyValuePair<string, TreeNode> kvp in _targetNodesToUpdate) {
				var elem = this.GetElement(_targetModel, kvp.Value);
				
				var parentNames = targetDependencies.ListMastersFullnames(kvp.Key);
				foreach (string parentName in parentNames) {
					TreeNode parentNode = null;
					if (_targetNodeCache.TryGetValue(parentName, out parentNode) && parentNode.BackColor == Color.Blue) {
						if (!toRemove.Contains(elem.Oid)) {
							toRemove.Add(elem.Oid);
							break;
						}
					}
				}
			}
			// ho nella List tmp tutti gli oid degli elementi con padre blu
			if (toRemove.Count > 0) {
				targetDependencies.DeleteElements(toRemove.ToArray());
			}
		}

		private void DropTargetMissingElements(List<long> removed, Query.Update targetDependencies)
		{
			List<long> oids = new List<long>();
			foreach (KeyValuePair<string, TreeNode> kvp in _targetNodesToUpdate) {
				var elem = this.GetElement(_targetModel, kvp.Value);
				if (kvp.Value.BackColor == Color.Blue && !removed.Contains(elem.Oid))
					oids.Add(elem.Oid);
			}
			// ho nella List Oid, gli elementi blu restanti
			if (oids.Count > 0) {
				targetDependencies.DeleteElements(oids.ToArray());
			}
		}

		private void UpdateDifferentOnes(Query.Update sourceDependencies, Query.Update targetDependencies)
		{
			Dictionary<long, string> aux = new Dictionary<long, string>();
			List<string> prnt = new List<string>();
			List<long> aux2 = new List<long>();
			TreeNode tN;

			foreach (KeyValuePair<string, TreeNode> kvp in _sourceNodesToUpdate) {
				bool isBlue = false;
				if (kvp.Value.BackColor == Color.Red) {
					prnt = sourceDependencies.ListMastersFullnames(kvp.Key);
					foreach (string str in prnt)
						if (_sourceNodeCache.TryGetValue(str, out tN)) {
							var elem = this.GetElement(_sourceModel, tN);
							if (aux.Count == 0 || !aux2.Contains(elem.Oid))
								if (tN.BackColor == Color.Blue) {
									isBlue = true;
									break;
								}
						}
				}
				if (isBlue == false)
					if (_targetNodeCache.TryGetValue(kvp.Key, out tN)) {
						var elS = this.GetElement(_sourceModel, kvp.Value);
						var elT = this.GetElement(_targetModel, tN);
						aux.Add(elT.Oid, elS.SqlCreate);
						aux2.Add(elS.Oid);
					}
				prnt.Clear();
			}// a questo punto in aux si trovano gli elementi da aggiornare

			if (aux.Count > 0) {
				Dictionary<long, string>.KeyCollection auxKeyCol = aux.Keys;
				long[] missingOids = auxKeyCol.ToArray();
				missingOids = this.sort(missingOids, targetDependencies);
				StringBuilder tmpstr = new StringBuilder();
				string hstoreUpdateCode;
				int cont = aux.Count;
				for (int i = 0; i < missingOids.Length; i++) {
					if (aux.TryGetValue(missingOids[i], out hstoreUpdateCode)) {
						if (i == missingOids.Length - 1) {
							tmpstr.Append(missingOids[i]);
							tmpstr.Append(" => \"");
							tmpstr.Append(hstoreUpdateCode + "\"");
						} else {
							tmpstr.Append(missingOids[i]);
							tmpstr.Append(" => \"");
							tmpstr.Append(hstoreUpdateCode + "\", ");
						}
					}
				}
				hstoreUpdateCode = tmpstr.ToString();

				/// necessario il drop degli elementi, le tabelle non possono essere aggiornate se non viene droppata a cascata prima
				targetDependencies.DeleteElements(missingOids);
				targetDependencies.UpdateElements(hstoreUpdateCode);
			}
			aux.Clear();
			aux2.Clear();
		}

		private void CreateElementsNotDependingOnSchemaToDrop(Query.Update sourceDependencies, Query.Update targetDependencies)
		{
			Dictionary<long, string> aux = new Dictionary<long, string>();
			List<string> prnt = new List<string>();
			TreeNode tN;
			StringBuilder strSql = new StringBuilder();
			bool genBlue;

			foreach (KeyValuePair<string, TreeNode> kvp in _sourceNodesToUpdate) {
				StringBuilder constructor = new StringBuilder();
				constructor.Append("\n" + kvp.Key + " ha come genitori:");
				genBlue = false;
				if (kvp.Value.BackColor == Color.Blue) {
					prnt = sourceDependencies.ListMastersFullnames(kvp.Key);
					foreach (string str in prnt) {
						constructor.Append("\n - " + str);
						if (_sourceNodeCache.TryGetValue(str, out tN)) {
							var elem = this.GetElement(_sourceModel, tN);
							if (aux.Count == 0 || !aux.ContainsKey(elem.Oid))
								if (tN.BackColor == Color.Blue) {
									constructor.Append("\n -> genitore blu trovato");
									genBlue = true;
									break;
								}
						}
					}

					if (genBlue == false)
						if (_sourceNodeCache.TryGetValue(kvp.Key, out tN)) {
							constructor.Append("\n -> Non sono tutti ha genitori blu (viene aggiunto)");
							var elS = this.GetElement(_sourceModel, kvp.Value);
							aux.Add(elS.Oid, elS.SqlCreate);
						}
					prnt.Clear();
				}
				MessageBox.Show(constructor.ToString());
			}
			if (aux.Count > 0) {
				StringBuilder mocofeo = new StringBuilder();
				mocofeo.Append("gli elementi blu che non hanno genitori blu sono:");
				foreach (KeyValuePair<long, string> kvp in aux) {
					strSql.Append(kvp.Value + "\n");
				}
				string stringona = strSql.ToString();
				mocofeo.Append(stringona);
				targetDependencies.CreateElements(stringona);
			}
			aux.Clear();
		}

		private void CreateElements(Query.Update dps, Query.Update dpt)
		{
			Dictionary<long, string> aux = new Dictionary<long, string>();
			List<string> prnt = new List<string>();
			TreeNode tN;
			int i;

			foreach (KeyValuePair<string, TreeNode> kvp in _sourceNodesToUpdate) {
				bool genBlue = false;
				prnt = dps.ListMastersFullnames(kvp.Key);
				foreach (string gen in prnt)
					if (_sourceNodeCache.TryGetValue(gen, out tN)) {
						var elem = this.GetElement(_sourceModel, tN);
						if (aux.Count == 0 || !aux.ContainsKey(elem.Oid))
							if (tN.BackColor == Color.Blue) {
								genBlue = true;
								break;
							}
					}

				if (genBlue == true) {
					var elS = this.GetElement(_sourceModel, kvp.Value);
					aux.Add(elS.Oid, elS.SqlCreate);
				}
				prnt.Clear();

			}
			if (aux.Count > 0) {
				StringBuilder SQL = new StringBuilder();
				Dictionary<long, string>.KeyCollection kcol = aux.Keys;
				long[] arOids = kcol.ToArray();
				arOids = this.sort(arOids, dps);
				string straux;

				for (i = 0; i < arOids.Length; i++) {
					if (aux.TryGetValue(arOids[i], out straux))
						SQL.Append(straux + "\n");
				}
				dpt.CreateElements(SQL.ToString());
			}
			aux.Clear();
		}

		#endregion

		// ordina gli elementi dall'array in modo di avere la forma genitori-figli
		private long[] sort(long[] elem, Query.Update dependencies)
		{
			List<long> gen = new List<long>();
			long[] aux = new long[elem.Length];
			int i, j, l;
			int n = elem.Length;
			long tmp;
			for (i = 0; i < n; i++) {
				long cld = elem[i];
				gen = dependencies.ListMastersOid(elem[i]);
				int k = 0;
				for (j = i + 1; j < n; j++)
					if (gen.Contains(elem[j]))
						aux[k++] = elem[j];

				if (k > 0) {
					aux = sort(aux, dependencies);
					for (j = 0; j < k; j++) {
						for (l = i + 1; l < n; l++)
							if (elem[l] == aux[j])
								break;
						if (i + j != l) {
							tmp = elem[i + j];
							elem[i + j] = elem[l];
							elem[l] = tmp;
						}
					}
					gen.Clear();
					for (l = i + k; l < n; l++)
						if (cld == elem[l])
							break;
					i = i + k;
					if (i != l) {
						tmp = elem[i];
						elem[i] = elem[l];
						elem[l] = tmp;
					}
				}
			}
			return elem;
		}


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