using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BO.DotPgDiff.Query
{
    static class Catalog
    {
        public static class Params
        {
            public static readonly string Database = "@db";
            public static readonly string Schema = "@schema";
            public static readonly string Table = "@table";
        }

        public static readonly string Database = string.Format(
            @"SELECT d.oid, d.datname, u.rolname 
            FROM pg_database d
            JOIN pg_roles u ON d.datdba = u.oid
            WHERE d.datname = {0}", Params.Database);

        public static readonly string BaseTypes =
            @"SELECT oid, name, owner, real_type_oid FROM deps.t_base_type";

        public static readonly string Schemas =
            @"SELECT oid, name, owner, sql_drop, sql_create FROM deps.t_schema ORDER BY name";


        public static readonly string Tables = string.Format(
            @"SELECT oid, name, fullname, owner, sql_drop, sql_create
			FROM deps.t_composite_type
			WHERE object_kind = 'T' AND schema_oid = {0}
			ORDER BY name", Params.Schema);

        public static readonly string Views = string.Format(
			@"SELECT oid, name, fullname, owner, sql_drop, sql_create
			FROM deps.t_composite_type
			WHERE object_kind = 'V' AND schema_oid = {0}
			ORDER BY name", Params.Schema);

        public static readonly string CompositeTypes = string.Format(
			@"SELECT oid, name, fullname, owner, sql_drop, sql_create
			FROM deps.t_composite_type
			WHERE object_kind = 'C' AND schema_oid = {0}
			ORDER BY name", Params.Schema);


        public static readonly string Functions = string.Format(
			@"SELECT oid, name, fullname, signature, owner, sql_drop, sql_create || ';'
			FROM deps.t_function
			WHERE schema_oid = {0}
			ORDER BY name", Params.Schema);

        public static readonly string Operators = string.Format(
			@"SELECT oid, name, fullname, owner, sql_drop, sql_create
			FROM deps.t_operator
			WHERE schema_oid = {0}
			ORDER BY name", Params.Schema);

        public static readonly string Columns = string.Format(
            @"SELECT 0::oid, a.attname, a.atttypid, NOT a.attnotnull, NULLIF(a.atttypmod, -1) - CASE WHEN a.attlen = -1 THEN 4 ELSE 0 END, d.adsrc
            FROM pg_attribute a
            LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
            WHERE NOT a.attisdropped AND a.attnum > 0 AND a.attrelid = {0}
            ORDER BY a.attnum", Params.Table);
    }
}
