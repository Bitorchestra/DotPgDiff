using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Windows.Forms;

namespace BO.DotPgDiff.Query
{
    class Update : IDisposable
    {
        private string _dbName;
        private NpgsqlConnection _conn;
        private windowSql wsql = new windowSql();

        public Update(NpgsqlConnectionStringBuilder connectionInfo)
        {
            connectionInfo.PreloadReader = true;
            _dbName = connectionInfo.Database;
            _conn = new NpgsqlConnection(connectionInfo.ConnectionString);
            _conn.Open();
        }

        public Update(string host, string port, string user, string password, string database)
            : this(
                new NpgsqlConnectionStringBuilder {
                    Host = host,
                    Port = Int32.Parse(port),
                    UserName = user,
                    Password = password,
                    Database = database
                })
        {
        }

        public void Dispose()
        {
            if (_conn != null) {
                if (_conn.State == System.Data.ConnectionState.Open) {
                    _conn.Close();
                }
                _conn = null;
            }
        }

        public void prLoad()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = Catalog.prLoad;
            cmd.ExecuteNonQuery();
        }

        /**************************************/
        //esiste Deps-Last_load?
        public string ExistsLastLoad()
        {
            string str = null;
            var cmd = _conn.CreateCommand();
            cmd.CommandText = Catalog.prLoad;
            cmd.ExecuteNonQuery();

            cmd.CommandText = Catalog.DepsLastLoad;
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    try
                    {
                        str = (string)reader[0];
                        //cmd.CommandText = Catalog.Update;///////////////////////////////
                        //cmd.ExecuteNonQuery();
                        
                    }
                    catch (Exception e)
                    {
                        //cmd.CommandText = Catalog.Insert;
                        //cmd.ExecuteNonQuery();
                        //Dispose();
                        return null;
                    }
                }
            }
            //Dispose();
            return str;
        }

        /**************************************/
        //aggiorna Deps-Last_load
        public string updateDeps()
        {
            string str = null;
            var cmd = _conn.CreateCommand();
            cmd.CommandText = Catalog.Update;
            cmd.ExecuteNonQuery();

            cmd.CommandText = Catalog.DepsLastLoad;
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    str = (string)reader[0];
                }
            }
            Dispose();
            return str;
        }
        
        /**************************************/
        //crea Deps-Last_load
        public string create()
        {
            string str = null;
            var cmd = _conn.CreateCommand();
            cmd.CommandText = Catalog.Insert;
            cmd.ExecuteNonQuery();
            cmd.CommandText = Catalog.DepsLastLoad;
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    str = (string)reader[0];
                }
            }
            Dispose();
            return str;
        }

        /**************************************/
        //preleva i fullname dei genitori
        public List<string> getMastersFullnames(string fullnameElem)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.parentsFn, fullnameElem);
                
                List<string> list = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add((string)reader[0]);
                }
                return list;
            }
        }

        /************************************** /
        //preleva i fullname dei genitori
        public List<string> getMastersFullnames(long oidElem)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.parentsOid, oidElem);

                List<string> list = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add((string)reader[0]);
                }
                return list;
            }
        }*/

        /* *************************************
        //preleva i fullname dei figli
        public List<string> getChildrenFullnames(long elem)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.childrens, elem);

                List<string> list = new List<string>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add((string)reader[0]);
                }
                return list;
            }
        }*/

        /**************************************/
        //ritorna una lista con gli OID dei genitori dell'elemento
        public List<long> getMastersOid(long elemOid)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.oidParents, elemOid);

                List<long> list = new List<long>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add((long)reader[0]);
                }
                return list;
            }
        }

        /**************************************/
        //
        public void dbnrdb(long[] ar)
        {
            string str = null;
            var obj = new windowSql();
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.dbnrdb, String.Join(",", ar));
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        str = (string)reader[0];
                }
                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("Drop in target degli elementi blu e rossi che dipendono da almeno un blu segnati in target\nvuoi visualizzare il codice?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    obj.setText(str);
                    obj.Show(); /////////////////  FARE IN MODO CHE IL PROGRAMA RIMANGA IN ATTESA PER MODIFICARE IL CODICE
			    }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
			    }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }
                obj.Close();
      //          cmd.CommandText = wsql.getText();
                //cmd.ExecuteNonQuery();
            }
        }

        /**************************************/
        //
        public void dbr(long[] ar)
        {
            string str = null;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.dbnrdb, String.Join(",", ar));
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        str = (string)reader[0];
                }
                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("Drop in target degli elementi blu restanti segnati in target\nvuoi visualizzare il codice?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    MessageBox.Show("visializzo il codice e posso modificarlo");
                }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
                }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }
                cmd.CommandText = str;
                //cmd.ExecuteNonQuery();
            }
        }

        //
        public void urdbr(long[] ar)
        {
            string str = null;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.dbnrdb, String.Join(",", ar));
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        str = (string)reader[0];
                }
                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("Aggiornamento elementi rossi che non dipendono da nessun elemento blu\ncodice sql di creazione pronto\nvuoi visualizzare il codice di drop?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    MessageBox.Show("visializzo il codice e posso modificarlo");
                }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
                }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }
                cmd.CommandText = str;
                //cmd.ExecuteNonQuery();
            }
        }

        /**************************************/
        //
        public void ur(string auxStr, long[] arOids)
        {
            string str = null;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = string.Format(Catalog.ur, auxStr);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        str = (string)reader[0];
                }// str ha il codice di aggiornamento
                this.urdbr(arOids);

                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("vuoi visualizzare il codice di creazione?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    MessageBox.Show("visializzo il codice e posso modificarlo");
                }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
                }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }

                cmd.CommandText = str;
                //cmd.ExecuteNonQuery();
            }
        }

        public void bnb(string str)
        {
            using (var cmd = _conn.CreateCommand())
            {
                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("creazione in target degli elementi blu segnati in source che non dipendono da nessun blu\nvuoi visualizzare il codice?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    MessageBox.Show("visializzo il codice e posso modificarlo");
                }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
                }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }
                cmd.CommandText = str;
                //cmd.ExecuteNonQuery();
            }
        }

        public void bgb(string str)
        {
            using (var cmd = _conn.CreateCommand())
            {
                MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
                DialogResult dr1 = MessageBox.Show("creazione in target degli elementi blu e rossi segnati in source che dipendono da almeno un blu\nvuoi visualizzare il codice?", "", MessageBoxButtons.AbortRetryIgnore);
                if (dr1 == DialogResult.Abort)
                {
                    MessageBox.Show("visializzo il codice e posso modificarlo");
                }
                else if (dr1 == DialogResult.Retry)
                {
                    MessageBox.Show("sto eseguendo il codice");
                }
                else if (dr1 == DialogResult.Ignore)
                {
                    MessageBox.Show("non faccio niente e vado al seguente processo");
                }
                
                cmd.CommandText = str;
                //cmd.ExecuteNonQuery();
            }
        }
    }
}
