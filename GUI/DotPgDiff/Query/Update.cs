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

		public void RebuildDependencies()
		{
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = Catalog.DepsRebuild;
				cmd.CommandTimeout = 0;
				cmd.ExecuteNonQuery();
			}
			this.UpdateLastLoadTime();
		}

		public DateTime? GetLastLoadTime()
		{
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = Catalog.DepsGetLoadTime;
				using (var reader = cmd.ExecuteReader()) {
					if (reader.Read() && reader.HasRows) {
						try {
							return (DateTime?)reader[0];
						} catch { }
					}
				}
			}
			return null;
		}

		private void UpdateLastLoadTime()
		{
			using (var cmd = _conn.CreateCommand()) {
				if (this.GetLastLoadTime().HasValue)
					cmd.CommandText = Catalog.DepsUpdateLoadTime;
				else
					cmd.CommandText = Catalog.DepsAddLoadTime;
				cmd.ExecuteNonQuery();
			}
		}

		public List<string> ListMastersFullnames(string elementFullname)
		{
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = string.Format(Catalog.ParentNames, elementFullname);

				List<string> list = new List<string>();
				using (var reader = cmd.ExecuteReader()) {
					while (reader.Read())
						list.Add((string)reader[0]);
				}
				return list;
			}
		}

		public List<long> ListMastersOid(long elementOid)
		{
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = string.Format(Catalog.ParentOids, elementOid);

				List<long> list = new List<long>();
				using (var reader = cmd.ExecuteReader()) {
					while (reader.Read())
						list.Add((long)reader[0]);
				}
				return list;
			}
		}

		private void ExecuteSql(string SQL, string message)
		{
			MsgBoxUtil.HackMessageBox("Yes", "Run", "Abort");
			DialogResult dr1 = MessageBox.Show(message, "", MessageBoxButtons.AbortRetryIgnore);
			MsgBoxUtil.UnHackMessageBox();
			if (dr1 == DialogResult.Abort) {
				// MessageBox.Show("visializzo il codice e posso modificarlo");
				using (var view = new CodeSql(SQL)) {
					if (view.ShowDialog() == DialogResult.OK)
						SQL = view.SQL;
					else
						return;
				}
			} else if (dr1 == DialogResult.Retry) {
				//MessageBox.Show("sto eseguendo il codice");
			} else if (dr1 == DialogResult.Ignore) {
				return;
				//MessageBox.Show("non faccio niente e vado al seguente processo");
			}
				using (var cmd = _conn.CreateCommand()) {
					cmd.CommandText = SQL;
					//cmd.ExecuteNonQuery();
				}
		}

		public void DeleteElements(long[] oids)
		{
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = string.Format(Catalog.GetDropChain, String.Join(",", oids));
				using (var reader = cmd.ExecuteReader()) {
					if (reader.Read() && reader.HasRows) {
						string SQL = (string)reader[0];
						this.ExecuteSql(SQL, "Drop in target degli elementi blu restanti segnati in target\nvuoi visualizzare il codice ?");
					} else {
						MessageBox.Show("Codice non generato !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
				
			}
		}

		public void UpdateElements(string hstoreUpdateCode)
		{
			string SQL = null;
			using (var cmd = _conn.CreateCommand()) {
				cmd.CommandText = string.Format(Catalog.GetUpdateChain, hstoreUpdateCode);
				using (var reader = cmd.ExecuteReader()) {
					if (reader.Read() && reader.HasRows) {
						SQL = (string)reader[0];
						this.ExecuteSql(SQL, "Aggiornamento elementi\nvuoi visualizzare il codice ?");
					} else {
						MessageBox.Show("Codice non generato !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		public void CreateElements(string SQL)
		{
			this.ExecuteSql(SQL, "creazione in target degli elementi blu segnati in source che non dipendono da nessun blu\nvuoi visualizzare il codice ?");
		}
	}
}
