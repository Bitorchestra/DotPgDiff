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
        private char[] str1 = { '1', '2', '3', '4', '5' , '6' , '7' , '8' , '9' , '0' , '.' , '+' };
        private char[] str2 = { ':' };
        private Dictionary<string, TreeNode> TDicSource = new Dictionary<string, TreeNode>();
        private Dictionary<string, TreeNode> TDicTarget = new Dictionary<string, TreeNode>();
        Dictionary<string, TreeNode> dicS = new Dictionary<string, TreeNode>();
        Dictionary<string, TreeNode> dicT = new Dictionary<string, TreeNode>();

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

		private void sourceConnect_Click(object sender, EventArgs ex)
		{
            try {
                _conStrSource = this.GetConnectionString();
            } catch (Exception e) {
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK);
			}
            var model = this.LoadTree(sourceTree, sourceDescription, _conStrSource);
            if (model != null)
            {
                var dp = new Query.Loader(_conStrSource);

                prLoadSource.Text = "Last Update Schema Deps: " + cutString(dp);
                prLoadSource.Enabled = true;
                btnCopySource.Enabled = true;
                _sourceModel = model;
            }
            btnDiff.Enabled = (_sourceModel != null && _targetModel != null);
            btnSync.Enabled = (_sourceModel != null && _targetModel != null);
		}

		private void targetConnect_Click(object sender, EventArgs ex)
		{
            try
            {
                _conStrTarget = this.GetConnectionString();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK);
            }
            var model = this.LoadTree(targetTree, targetDescription, _conStrTarget);
            if (model != null)
            {
                var dp = new Query.Loader(_conStrTarget);
                prLoadTarget.Text = "Last Update Schema Deps: " + cutString(dp);
                prLoadTarget.Enabled = true;
                btnCopyTarget.Enabled = true;
                _targetModel = model;
            }
            btnDiff.Enabled = (_sourceModel != null && _targetModel != null);
            btnSync.Enabled = (_sourceModel != null && _targetModel != null);
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
			//try {
				var elm = this.GetElement(_targetModel, e.Node);
				SQLTarget.Text = "-- TARGET\r\n" + GetElementSource(elm);
			//} catch { }
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

        private void prLoadSource_Click(object sender, EventArgs e)
        {
            Cursor = System.Windows.Forms.Cursors.WaitCursor;
            prLoad(_conStrSource);
            Cursor = System.Windows.Forms.Cursors.Default;
        }

        private void prLoadTarget_Click(object sender, EventArgs e)
        {
            Cursor = System.Windows.Forms.Cursors.WaitCursor;
            prLoad(_conStrTarget);
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
                if (_conStr == null)
                {
					return null;
				}

                desc.Text = string.Format("{0}@{1}@{2}", _conStr.UserName, _conStr.Database, _conStr.Host);
				tv.Nodes.Clear();

                using (var c = new Query.Loader(_conStr))
                {
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

        private void syncLoadTree()
        {
            _targetModel = null;
            var model = this.LoadTree(targetTree, targetDescription, _conStrTarget);
            if (model != null)
            {
                var dp = new Query.Loader(_conStrTarget);
                prLoadTarget.Text = "Last Update Schema Deps: " + cutString(dp);
                prLoadTarget.Enabled = true;
                btnCopyTarget.Enabled = true;
                _targetModel = model;
            }
            btnDiff.Enabled = (_sourceModel != null && _targetModel != null);
            btnSync.Enabled = (_sourceModel != null && _targetModel != null);
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
            foreach (TreeNode tn1 in tView.Nodes)
            {
                foreach (TreeNode tn2 in tn1.Nodes)
                {
                    foreach (TreeNode tn3 in tn2.Nodes)
                    {
                        if (tNode1.Text == tn3.Text)
                        {
                            if (tn3.BackColor == Color.Green)
                            {
                                return 1;  // green
                            }
                            else if (tn3.BackColor == Color.Red)
                            {
                                return 2;  // red
                            }
                            else
                            {
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

		private void btnDiff_Click(object sender, EventArgs e)
		{
            this.DecorateDifferences(sourceTree.Nodes, targetTree.Nodes);
			this.DecorateDifferences(sourceTree.Nodes);
			this.DecorateDifferences(targetTree.Nodes);
            dicS.Clear();
            dicT.Clear();
            foreach (TreeNode tn1 in sourceTree.Nodes)
            {
                tn1.Checked = false;
                dicS.Add(tn1.Text, tn1);
                foreach (TreeNode tn2 in tn1.Nodes)
                {
                    tn2.Checked = false;
                    dicS.Add(tn1.Text + ".." + tn2.Text, tn2);
                    foreach (TreeNode tn3 in tn2.Nodes)
                    {
                        tn3.Checked = false;
                        dicS.Add(tn3.Text, tn3);
                    }
                }
            }
            foreach (TreeNode tn1 in targetTree.Nodes)
            {
                tn1.Checked = false;
                dicT.Add(tn1.Text, tn1);
                foreach (TreeNode tn2 in tn1.Nodes)
                {
                    tn2.Checked = false;
                    dicT.Add(tn1.Text + ".." + tn2.Text, tn2);
                    foreach (TreeNode tn3 in tn2.Nodes)
                    {
                        tn3.Checked = false;
                        dicT.Add(tn3.Text, tn3);
                    }
                }
            }
		}

		#endregion

        #region TreeCheckBox

        private void sourceTree_AfterCheck(object sender, TreeViewEventArgs e)//////////////////////////MODIFICAAAAAAAAAAAAAAAAAA
        {
            sourceTree.AfterCheck -= sourceTree_AfterCheck;
            //MessageBox.Show("sourceTree_AfterCheck");
            if (e.Node.BackColor == Color.Green)
                e.Node.Checked = false;
            else
                ricorNodesCheck(e.Node);
            TreeNode tn;
            if (e.Node.BackColor == Color.Red)
            {
                if (dicT.TryGetValue(e.Node.Text, out tn))
                {
                    if (tn.Checked != e.Node.Checked)
                        tn.Checked = e.Node.Checked;
                }
                else if (e.Node.Parent != null && e.Node.Nodes.Count > 0)
                {
                    if (dicT.TryGetValue(e.Node.Parent.Text + ".." + e.Node.Text, out tn))
                        if (tn.Checked != e.Node.Checked)
                            tn.Checked = e.Node.Checked;
                }
            }
                // FARE LA STESSA ACTION nel targetTree

            // se e solo se il metodo AfterCheck inserice o toglie elementi in/da TDicSource allora:
            //btnSync.Enabled = (TDicSource.Count > 0 && TDicTarget.Count > 0);
            sourceTree.AfterCheck += sourceTree_AfterCheck;
        }
        private void targetTree_AfterCheck(object sender, TreeViewEventArgs e)/////////////////////////////////////////////////
        {
            targetTree.AfterCheck -= targetTree_AfterCheck;
            //MessageBox.Show("targetTree_AfterCheck");
            if (e.Node.BackColor == Color.Green)
                e.Node.Checked = false;
            else
                ricorNodesCheck(e.Node);
            TreeNode tn;
            if (e.Node.BackColor == Color.Red)
            {
                if (dicS.TryGetValue(e.Node.Text, out tn))
                {
                    if (tn.Checked != e.Node.Checked)
                        tn.Checked = e.Node.Checked;
                }
                else if (e.Node.Parent != null && e.Node.Nodes.Count > 0)
                {
                    if (dicS.TryGetValue(e.Node.Parent.Text + ".." + e.Node.Text, out tn))
                        if (tn.Checked != e.Node.Checked)
                            tn.Checked = e.Node.Checked;
                }
            }

            // se e solo se il metodo AfterCheck inserice o toglie elementi in/da TDicTarget allora:
            //btnSync.Enabled = (TDicSource.Count > 0 && TDicTarget.Count > 0);
            targetTree.AfterCheck += targetTree_AfterCheck;
        }

        private void ricorNodesCheck(TreeNode eNode)
        {
            if (eNode.Parent == null)
                RicorsNoParent(eNode);
            else if (eNode.Parent != null && eNode.Nodes.Count > 0)
            {
                RicorsNoParent(eNode);
                if (eNode.Checked == false)
                    eNode.Parent.Checked = false;
                else
                    RicorsParent(eNode);
            }
            else
                RicorsParent(eNode);
        }

        // Segna o toglie il segno dai checkBox a seconda della scelta
        private void RicorsNoParent(TreeNode eNode)
        {
            foreach (TreeNode oNodo in eNode.Nodes)
            {
                if (oNodo.BackColor != Color.Green)
                {
                    oNodo.Checked = eNode.Checked;
                    if (oNodo.Nodes.Count > 0)//si puo anche togliere questo controllo, perche ogni oNodo ha como minimo un nodo figlio
                        RicorsNoParent(oNodo);
                }
                else
                    oNodo.Checked = false;
            }
        }

        private void RicorsParent(TreeNode eNode)
        {
            if (eNode.Parent != null)
            {
                if (eNode.Checked == false)
                {
                    eNode.Parent.Checked = false;
                    eNode.Parent.Parent.Checked = false;
                }
                else
                {
                    bool result = true;
                    foreach (TreeNode oNodo in eNode.Parent.Nodes)
                        if (oNodo.BackColor != Color.Green)
                            if (oNodo.Checked == false)
                            {
                                result = false;
                                break;
                            }
                    eNode.Parent.Checked = result;
                    if (eNode.Parent.Checked == true)
                        RicorsParent(eNode.Parent);
                }
            }
        }

        #endregion

        #region Syncronize

        private void btnSync_Click(object sender, EventArgs e)
        {
            DialogResult dr1 = MessageBox.Show("Are you sure?", "", MessageBoxButtons.YesNo);
            if (dr1 == DialogResult.Yes)
            {
                Cursor = System.Windows.Forms.Cursors.WaitCursor;
                
                //percorro l'albero source e inserisco in TDicSource tutti gli elementi il cui CheckBox e segnato
                AddElementsOnTDic(sourceTree, TDicSource);

                //percorro l'albero Target e inserisco in TDicTarget tutti gli elementi il cui CheckBox e segnato
                AddElementsOnTDic(targetTree, TDicTarget);

                // in caso che qualche elemento di TDicSource/TDicTarget dipenda da altri elementi che non sono stati segnati
                // allora questi verrano aggiunti
                nodParentsSource();
                nodParentsTarget();

                /************************   INNIZIA IL PROCESSO DI AGGIORNAMENTO SUL DB   ************************/
                var dps = new Query.Update(_conStrSource);
                var dpt = new Query.Update(_conStrTarget);
                List<long> tmp = new List<long>();

                /////// -	Drop degli elementi (blu, e rossi che dipendono da blu) contenuti in TDicTarget
                dropBlunRedDipBluTarget(ref tmp, dpt);

                /////// -	Drop degli elementi blu restanti in TDicTarget
                dropBluRest(tmp, dpt);
                tmp.Clear();
                
                /////// -	Aggiornare gli elementi (rossi) contenuti in TDicSource che non dipendono da un elemento blu
                updateRed(dps, dpt);

                /////// -   Creare gli elementi blu che non dipendono da blu, contenuti in TDicSource
                createBluNonBlu(dps, dpt);
                
                /////// -	Creare gli elementi blu, e rossi che dipendono almeno un blu, contenuti in TDicSource
                createBlunRedDipBluSource(dps, dpt);

                /////// -	Eseguire la pr_load in target
//                MessageBox.Show("pr_load e caricamento\n           su Target");
//                dpt.prLoad();
                /////// -	Eseguire il LoadTree in modo da aggiornare source e target con i nuovi dati
//                syncLoadTree();
                /////// -	Eseguire l’evento di colorazione in modo da far vedere le nuove differenze
//                MessageBox.Show("Diff!");
//                this.btnDiff_Click(null, null);

                /*******************************   FINE DELL'AGGIORNAMENTO SUL DB   ******************************/
                RemoveChecks(sourceTree);
                RemoveChecks(targetTree);
                TDicSource.Clear();
                TDicTarget.Clear();
                Cursor = System.Windows.Forms.Cursors.Default;
            }
            else if (dr1 == DialogResult.No)
            {
                DialogResult dr2 = MessageBox.Show("Remove the check marks on the source tree?", "", MessageBoxButtons.YesNo);
                if (dr2 == DialogResult.Yes)
                {
                    RemoveChecks(sourceTree);
                }
                DialogResult dr3 = MessageBox.Show("Remove the check marks on the target tree?", "", MessageBoxButtons.YesNo);
                if (dr3 == DialogResult.Yes)
                {
                    RemoveChecks(targetTree);
                }
            }
            
        }

        private void AddElementsOnTDic(TreeView TView, Dictionary<string, TreeNode> TDic)
        {
            bool nodGreen;
            foreach (TreeNode oNodoA in TView.Nodes)
            {
                nodGreen = false;
                if (oNodoA.Nodes.Count > 0)
                {
                    foreach (TreeNode oNodoB in oNodoA.Nodes)
                        foreach (TreeNode oNodoC in oNodoB.Nodes)
                        {
                            if (oNodoC.Checked == true)
                                TDic.Add(oNodoC.Text, oNodoC);
                            if (oNodoC.BackColor == Color.Green)
                                nodGreen = true;
                        }
                    if (nodGreen == false && oNodoA.BackColor == Color.Blue && oNodoA.Checked == true)
                        TDic.Add(oNodoA.Text, oNodoA);
                }
                else
                    if (oNodoA.BackColor == Color.Blue && oNodoA.Checked == true)
                        TDic.Add(oNodoA.Text, oNodoA);
                
            }
        }

        private void RemoveChecks(TreeView TView)
        {
            foreach (TreeNode oNodoA in TView.Nodes)
            {
                oNodoA.Checked = false;
                if (oNodoA.Nodes.Count > 0)
                    foreach (TreeNode oNodoB in oNodoA.Nodes)
                    {
                        oNodoB.Checked = false;
                        foreach (TreeNode oNodoC in oNodoB.Nodes)
                            oNodoC.Checked = false;
                    }
            }
        }

        // 
        private void nodParentsSource()
        {
            Dictionary<string, TreeNode> dicAux = new Dictionary<string, TreeNode>();
            List<string> prnt = new List<string>();
            var dp = new Query.Update(_conStrSource);
            if (TDicSource.Count > 0)
            {
                foreach (KeyValuePair<string, TreeNode> kvps in TDicSource)
                {
                    // per ogni kvp inserisce in prnt tutti i fullname degli elementi dai quali dipende (deps.t_deps)
                    prnt = dp.getMastersFullnames(kvps.Key);

                    // cerca nel albero se alcun genitore è rosso o blu
                    // se è rosso o blu verifica se c'è già su TDicSource
                    // se non c'è, lo aggiunge
                    foreach (string str in prnt)
                    {
                        TreeNode auxNode;
                        if (dicS.TryGetValue(str, out auxNode))
                        {
                            if (auxNode.BackColor == Color.Red || auxNode.BackColor == Color.Blue) // se è rosso o blu viene aggiunto nella lista
                                if (!TDicSource.ContainsKey(str))
                                    if (dicAux.Count == 0 || !dicAux.ContainsKey(str))
                                        dicAux.Add(str, auxNode);
                        }
                    }
                    prnt.Clear();
                }
                if (dicAux.Count > 0)
                    foreach (KeyValuePair<string, TreeNode> kvp in dicAux)
                        TDicSource.Add(kvp.Key, kvp.Value);
            }
        }

        // 
        private void nodParentsTarget()
        {
            Dictionary<string, TreeNode> dicAux = new Dictionary<string, TreeNode>();
            List<string> prnt = new List<string>();
            var dp = new Query.Update(_conStrTarget);
            if (TDicTarget.Count > 0)
            {
                foreach (KeyValuePair<string, TreeNode> kvps in TDicTarget)
                {
                    if (kvps.Value.BackColor != Color.Blue)
                    {
                        // per ogni kvp inserisce in prnt tutti i fullname degli elementi dai quali dipende (deps.t_deps)
                        prnt = dp.getMastersFullnames(kvps.Key);

                        // cerca nel albero se alcun genitore è rosso o blu
                        // se è rosso o blu verifica se c'è già su TDicSource
                        // se non c'è, lo aggiunge
                        foreach (string str in prnt)
                        {
                            TreeNode auxNode;
                            if (dicT.TryGetValue(str, out auxNode))
                            {
                                if (auxNode.BackColor == Color.Red || auxNode.BackColor == Color.Blue) // se è rosso o blu viene aggiunto nella lista
                                    if (!TDicTarget.ContainsKey(str))
                                        if(dicAux.Count == 0 || !dicAux.ContainsKey(str))
                                            dicAux.Add(str, auxNode);
                            }
                        }
                        prnt.Clear();
                    }
                }
                if (dicAux.Count > 0)
                    foreach (KeyValuePair<string, TreeNode> kvp in dicAux)
                        TDicTarget.Add(kvp.Key, kvp.Value);
            }
        }


        private void dropBlunRedDipBluTarget(ref List<long> tmp, Query.Update dpt)
        {
            List<string> prnt = new List<string>();
            TreeNode tN;
            foreach (KeyValuePair<string, TreeNode> kvp in TDicTarget)
            {
                prnt = dpt.getMastersFullnames(kvp.Key);
                foreach (string str in prnt)
                    if (dicT.TryGetValue(str, out tN))
                        if (tN.BackColor == Color.Blue)
                        {
                            var elem = this.GetElement(_targetModel, kvp.Value);
                            if (!tmp.Contains(elem.Oid))
                            {
                                tmp.Add(elem.Oid);
                                break;
                            }
                        }
                prnt.Clear();
            }// ho nella List tmp tutti gli oid degli elementi che compiono le condizioni
            if (tmp.Count > 0)
            {
                long[] ar = tmp.ToArray();
                dpt.dbnrdb(ar);
            }
        }

        private void dropBluRest(List<long> tmp, Query.Update dpt)
        {
            List<long> oids = new List<long>();
            foreach (KeyValuePair<string, TreeNode> kvp in TDicTarget)
            {
                var elem = this.GetElement(_targetModel, kvp.Value);
                if (kvp.Value.BackColor == Color.Blue && !tmp.Contains(elem.Oid))
                    oids.Add(elem.Oid);
            }// ho nella List Oid, gli elementi blu restanti
            if (oids.Count > 0)
            {
                long[] ar = oids.ToArray();
                dpt.dbr(ar);
            }
            oids.Clear();
        }

        private void updateRed(Query.Update dps, Query.Update dpt)
        {
            Dictionary<long, string> aux = new Dictionary<long, string>();
            List<string> prnt = new List<string>();
            List<long> aux2 = new List<long>();
            TreeNode tN;

            foreach (KeyValuePair<string, TreeNode> kvp in TDicSource)
            {
                bool isBlue = false;
                if (kvp.Value.BackColor == Color.Red)
                {
                    prnt = dps.getMastersFullnames(kvp.Key);
                    foreach (string str in prnt)
                        if (dicS.TryGetValue(str, out tN))
                        {
                            var elem = this.GetElement(_sourceModel, tN);
                            if (aux.Count == 0 || !aux2.Contains(elem.Oid))
                                if (tN.BackColor == Color.Blue)
                                {
                                    isBlue = true;
                                    break;
                                }
                        }
                }
                if (isBlue == false)
                    if (dicT.TryGetValue(kvp.Key, out tN))
                    {
                        var elS = this.GetElement(_sourceModel, kvp.Value);
                        var elT = this.GetElement(_targetModel, tN);
                        aux.Add(elT.Oid, elS.SqlCreate);
                        aux2.Add(elS.Oid);
                    }
                prnt.Clear();
            }// a questo punto in aux si trovano gli elementi da aggiornare

            if (aux.Count > 0)
            {
                Dictionary<long, string>.KeyCollection auxKeyCol = aux.Keys;
                long[] arOids = auxKeyCol.ToArray();
                arOids = this.sort(arOids, dpt);
                StringBuilder tmpstr = new StringBuilder();
                string strOut;
                int cont = aux.Count;
                for (int i = 0; i < arOids.Length; i++)
                {
                    if(aux.TryGetValue(arOids[i], out strOut))
                    {
                        if (i == arOids.Length - 1)
                        {
                            tmpstr.Append(arOids[i]);
                            tmpstr.Append(" => \"");
                            tmpstr.Append(strOut + "\"");
                        }
                        else
                        {
                            tmpstr.Append(arOids[i]);
                            tmpstr.Append(" => \"");
                            tmpstr.Append(strOut + "\", ");
                        }
                    }
                }
                strOut = tmpstr.ToString();

                //dpt.dbr(arOids);     /// necessario il drop degli elementi, le tabelle non possono essere aggiornate se non viene droppata a cascata prima
                dpt.ur(strOut, arOids);
            }
            aux.Clear();
            aux2.Clear();
        }

        private void createBluNonBlu(Query.Update dps, Query.Update dpt)
        {
            Dictionary<long, string> aux = new Dictionary<long, string>();
            List<string> prnt = new List<string>();
            TreeNode tN;
            StringBuilder strSql = new StringBuilder();
            bool genBlue;

            foreach (KeyValuePair<string, TreeNode> kvp in TDicSource)
            {
                StringBuilder constructor = new StringBuilder();
                constructor.Append("\n" + kvp.Key + " ha come genitori:");
                genBlue = false;
                if (kvp.Value.BackColor == Color.Blue)
                {
                    prnt = dps.getMastersFullnames(kvp.Key);
                    foreach (string str in prnt)
                    {
                        constructor.Append("\n - " + str);
                        if (dicS.TryGetValue(str, out tN))
                        {
                            var elem = this.GetElement(_sourceModel, tN);
                            if (aux.Count == 0 || !aux.ContainsKey(elem.Oid))
                                if (tN.BackColor == Color.Blue)
                                {
                                    constructor.Append("\n -> genitore blu trovato");
                                    genBlue = true;
                                    break;
                                }
                        }
                    }
                
                    if (genBlue == false)
                        if (dicS.TryGetValue(kvp.Key, out tN))
                        {
                            constructor.Append("\n -> Non sono tutti ha genitori blu (viene aggiunto)");
                            var elS = this.GetElement(_sourceModel, kvp.Value);
                            aux.Add(elS.Oid, elS.SqlCreate);
                        }
                    prnt.Clear();
                }
                MessageBox.Show(constructor.ToString());
            }
            if(aux.Count > 0)
            {
                StringBuilder mocofeo = new StringBuilder();
                mocofeo.Append("gli elementi blu che non hanno genitori blu sono:");
                foreach (KeyValuePair<long, string> kvp in aux)
                {
                    strSql.Append(kvp.Value + "\n");
                }
                string stringona = strSql.ToString();
                mocofeo.Append(stringona);
                dpt.bnb(stringona);
            }
            aux.Clear();
        }

        private void createBlunRedDipBluSource(Query.Update dps, Query.Update dpt)
        {
            Dictionary<long, string> aux = new Dictionary<long, string>();
            List<string> prnt = new List<string>();
            TreeNode tN;
            int i;

            foreach (KeyValuePair<string, TreeNode> kvp in TDicSource)
            {
                bool genBlue = false;
                prnt = dps.getMastersFullnames(kvp.Key);
                foreach (string gen in prnt)
                    if (dicS.TryGetValue(gen, out tN))
                    {
                        var elem = this.GetElement(_sourceModel, tN);
                        if (aux.Count == 0 || !aux.ContainsKey(elem.Oid))
                            if (tN.BackColor == Color.Blue)
                            {
                                genBlue = true;
                                break;
                            }
                    }

                if (genBlue == true)
                {
                        var elS = this.GetElement(_sourceModel, kvp.Value);
                        aux.Add(elS.Oid, elS.SqlCreate);
                }
                prnt.Clear();
                
            }
            if(aux.Count > 0)
            {
                StringBuilder strSql = new StringBuilder();
                Dictionary<long,string>.KeyCollection kcol = aux.Keys;
                long[] arOids = kcol.ToArray();
                arOids = this.sort(arOids, dps);
                string straux;

                for (i = 0; i < arOids.Length; i++)
                {
                    if(aux.TryGetValue(arOids[i], out straux))
                        strSql.Append(straux + "\n");
                }
                dpt.bgb(strSql.ToString());
            }
            aux.Clear();
        }

        #endregion

        // ordina gli elementi dall'array in modo di avere la forma genitori-figli
        private long[] sort(long[] elem, Query.Update dp)
        {
            List<long> gen = new List<long>();
            long[] aux = new long[elem.Length];
            int i, j, l;
            int n = elem.Length;
            long tmp;
            for (i = 0; i < n; i++)
            {
                long cld = elem[i];
                gen = dp.getMastersOid(elem[i]);
                int k = 0;
                for (j = i + 1; j < n; j++)
                    if (gen.Contains(elem[j]))
                        aux[k++] = elem[j];

                if (k > 0)
                {
                    aux = sort(aux, dp);
                    for (j = 0; j < k; j++)
                    {
                        for (l = i + 1; l < n; l++)
                            if (elem[l] == aux[j])
                                break;
                        if (i + j != l)
                        {
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
                    if (i != l)
                    {
                        tmp = elem[i];
                        elem[i] = elem[l];
                        elem[l] = tmp;
                    }
                }
            }
            return elem;
        }

        private void prLoad(Npgsql.NpgsqlConnectionStringBuilder _conStr)
        {
            string str;
            try
            {
                var dp = new Query.Update(_conStr);
                str = dp.ExistsLastLoad();
                if (str != null)
                {
                    string NewStr = dp.updateDeps().TrimEnd(str1);
                    NewStr = NewStr.TrimEnd(str2);
                    prLoadSource.Text = "Last Update Schema Deps: " + NewStr;
                }
                else
                {
                    string NewStr = dp.create().TrimEnd(str1);
                    NewStr = NewStr.TrimEnd(str2);
                    prLoadSource.Text = "Last Update Schema Deps: " + NewStr;
                }
            }
            catch (Exception ecc)
            {
                //MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK);
            }
        }

        private string cutString(Query.Loader dp)
        {
            string NewStr = dp.TextPrLoadStart().TrimEnd(str1);
            NewStr = NewStr.TrimEnd(str2);
            return NewStr;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var varo = new windowSql();
            varo.setText("pompinchu");
            varo.Show();
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