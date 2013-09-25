using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Windows.Forms;

namespace BO.DotPgDiff.Query
{
    class Loader : IDisposable
    {
        private string _dbName;
        private NpgsqlConnection _conn;

        public Loader(NpgsqlConnectionStringBuilder connectionInfo)
        {
            connectionInfo.PreloadReader = true;
            _dbName = connectionInfo.Database;
            _conn = new NpgsqlConnection(connectionInfo.ConnectionString);
            _conn.Open();
        }

        public Loader(string host, string port, string user, string password, string database)
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

        public Model.Database Load()
        {
            var rt = new Model.Database();
            using (var cmd = _conn.CreateCommand()) {
                cmd.CommandText = Catalog.Database;
                cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Database, _dbName));
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        rt.Oid = (long)reader[0];
                        rt.Name = (string)reader[1];
                        rt.Owner = (string)reader[2];
                    }
                }
            }
            //*****************************************************************************************
            rt.Schemas = this.ListSchemas(rt);
            rt.BaseTypes = this.ListBaseTypes(rt);

            rt.BuildCache();

            return rt;
        }
        
        private List<T> LoadSet<T>(Action<NpgsqlCommand> prepareCommand, Func<NpgsqlDataReader, T> getElement)
        {
            var rt = new List<T>();
            using (var cmd = _conn.CreateCommand()) {
                prepareCommand.Invoke(cmd);
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        rt.Add(getElement.Invoke(reader));
                    }
                }
            }

            return rt;
        }

        private static string ToString(object o)
        {
            if (o == null || o == DBNull.Value)
                return null;
            return (string)o;
        }
        private static bool? ToBool(object o)
        {
            if (o == null || o == DBNull.Value)
                return null;
            return (bool?)o;
        }
        private static long? ToLong(object o)
        {
            if (o == null || o == DBNull.Value)
                return null;
            return (long?)o;
        }
        private static int? ToInt(object o)
        {
            if (o == null || o == DBNull.Value)
                return null;
            return (int?)o;
        }

        private List<Model.Schema> ListSchemas(Model.Database db)
        {
            var rt = this.LoadSet(
                cmd => cmd.CommandText = Catalog.Schemas,
                reader => new Model.Schema {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
                    Owner = (string)reader[2],
					SqlDrop = (string)reader[3],
					SqlCreate = (string)reader[4],
                    Parent = db
                }
            );

            foreach (var s in rt) {
                s.CompositeTypes = this.ListCompositeTypes(s);
                s.Tables = this.ListTables(s);
                s.Views = this.ListViews(s);
                s.Functions = this.ListFunctions(s);
                s.Operators = this.ListOperators(s);
            }

            return rt;
        }
        private List<Model.BaseType> ListBaseTypes(Model.Database db)
        {
            var rt = this.LoadSet(
                cmd => cmd.CommandText = Catalog.BaseTypes,
                reader => new Model.BaseType {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
                    Owner = (string)reader[2],
                    RealTypeOid = Loader.ToLong(reader[3]),
					Parent = db
                }
            );

            return rt;
        }

        private List<Model.CompositeType> ListCompositeTypes(Model.Schema schema)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.CompositeTypes;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Schema, schema.Oid));
                },
                reader => new Model.CompositeType {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
					FullName = (string)reader[2],
					Owner = (string)reader[3],
					SqlDrop = (string)reader[4],
					SqlCreate = (string)reader[5],
					Parent = schema
                }
            );
			/*
            foreach (var r in rt) {
                r.Columns = this.ListColumns(r);
            }*/

            return rt;
        }
        private List<Model.Table> ListTables(Model.Schema schema)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.Tables;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Schema, schema.Oid));
                },
                reader => new Model.Table {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
					FullName = (string)reader[2],
					Owner = (string)reader[3],
					SqlDrop = (string)reader[4],
					SqlCreate = (string)reader[5],
					Parent = schema
                }
            );
			/*
            foreach (var r in rt) {
                r.Columns = this.ListColumns(r);
            }*/

            return rt;
        }
        private List<Model.View> ListViews(Model.Schema schema)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.Views;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Schema, schema.Oid));
                },
                reader => new Model.View() {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
					FullName = (string)reader[2],
                    Owner = (string)reader[3],
					SqlDrop = (string)reader[4],
					SqlCreate = (string)reader[5],
                    Parent = schema
                }
            );
			/*
            foreach (var r in rt) {
                r.Columns = this.ListColumns(r);
            }*/

            return rt;
        }
        private List<Model.Function> ListFunctions(Model.Schema schema)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.Functions;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Schema, schema.Oid));
                },
                reader => new Model.Function() {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
					FullName = (string)reader[2],
					Signature = (string)reader[3],
                    Owner = (string)reader[4],
					SqlDrop = (string)reader[5],
					SqlCreate = (string)reader[6],
					Parent = schema
                }
			);

            return rt;
        }
        private List<Model.Operator> ListOperators(Model.Schema schema)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.Operators;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Schema, schema.Oid));
                },
                reader => new Model.Operator {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
					FullName = (string)reader[2],
                    Signature = (string)reader[3],
                    Owner = (string)reader[4],
					SqlDrop = (string)reader[5],
					SqlCreate = (string)reader[6],
					Parent = schema
				}	//oid, name, fullname, owner, sql_drop, sql_create
            );

            return rt;
        }


		/*
        private List<Model.Column> ListColumns(Model.CompositeType t)
        {
            var rt = this.LoadSet(
                cmd => {
                    cmd.CommandText = Catalog.Columns;
                    cmd.Parameters.Add(new NpgsqlParameter(Catalog.Params.Table, t.Oid));
                },
                reader => new Model.Column {
                    Oid = (long)reader[0],
                    Name = (string)reader[1],
                    TypeOid = (long)reader[2],
                    IsNullable = (bool)reader[3],
                    TypeLen = Loader.ToInt(reader[4]),
                    Default = Loader.ToString(reader[5]),
                    Parent = t
                }
            );

            return rt;
        }*/
    }
}