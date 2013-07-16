﻿using System;
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
			@"SELECT oid, name, fullname, signature, owner, sql_drop, sql_create
			FROM deps.t_operator
			WHERE schema_oid = {0}
			ORDER BY name", Params.Schema);

        public static readonly string Columns = string.Format(
            @"SELECT 0::oid, a.attname, a.atttypid, NOT a.attnotnull, NULLIF(a.atttypmod, -1) - CASE WHEN a.attlen = -1 THEN 4 ELSE 0 END, d.adsrc
            FROM pg_attribute a
            LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
            WHERE NOT a.attisdropped AND a.attnum > 0 AND a.attrelid = {0}
            ORDER BY a.attnum", Params.Table);

        public static readonly string DepsCreate =
        @"SELECT (SELECT m_created FROM bo.t_e_parameter WHERE t_parameter_id = 'DEPS-LAST-LOAD')::text";

        public static readonly string DepsLastLoad =
        @"SELECT (SELECT m_last_modified FROM bo.t_e_parameter WHERE t_parameter_id = 'DEPS-LAST-LOAD')::text";

        public static readonly string prLoad =
        @"SELECT deps.pr_load();";

        public static readonly string Insert =
        @"INSERT INTO bo.t_e_parameter (t_parameter_id, t_value) VALUES ('DEPS-LAST-LOAD','statecurrent_timestamp')";

        public static readonly string Update =
        @"UPDATE bo.t_e_parameter SET m_last_modified = clock_timestamp() WHERE t_parameter_id = 'DEPS-LAST-LOAD'";

        public static readonly string parentsFn =
            @"SELECT unnest(deps.hfa_fullname_masters_of(deps.fi_object_oid('{0}')))";

        /*public static readonly string parentsOid =
            @"SELECT unnest(deps.hfa_masters_of({0}))";

        public static readonly string childrens =
            @"SELECT unnest(deps.hfa_slaves_of({0}::oid))";*/

        public static readonly string oidParents =
            @"SELECT unnest(deps.hfa_oid_masters_of({0}))";

        public static readonly string dbnrdb =
            @"SELECT deps.ft_sql_drop_chain(ARRAY[ {0} ])";

        public static readonly string ur =
            @"SELECT deps.ft_sql_update('{0}'::hstore)";

        public static readonly string bnb =
            @"SELECT deps.ft_sql_create_chain(Array[ {0} ])";
    }
}
