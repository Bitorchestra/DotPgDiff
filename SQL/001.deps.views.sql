-- ----------------------------------------------------------------------------------
-- SCHEMA: deps
--DROP SCHEMA deps CASCADE;
CREATE SCHEMA deps
;

CREATE OR REPLACE FUNCTION deps.fa_iif(boolean, anyelement, anyelement)
  RETURNS anyelement AS
$$
	SELECT
		CASE
			WHEN ($1)
			THEN $2
			ELSE $3
		END;
$$ LANGUAGE SQL 
	IMMUTABLE;

-- will be redefined in next source file
CREATE OR REPLACE FUNCTION deps.ft_object_fullname(object_oid oid) RETURNS TEXT
AS $$
	SELECT null::TEXT;
$$ LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;
CREATE OR REPLACE FUNCTION deps.hft_fk_action(text) RETURNS TEXT
AS $$
	SELECT null::TEXT;
$$ LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;

CREATE TABLE deps.t_deps (
	oid_master	oid
	,oid_cascades_to	oid
);

-- ----------------------------------------------------------------------------------
-- SCHEMAs

CREATE TABLE deps.t_schema (
	oid 		oid NOT NULL
	,name		name NOT NULL
	,owner		name NOT NULL
	,sql_drop	text
	,sql_create	text
	
	,CONSTRAINT pk_deps_schema PRIMARY KEY(oid)
	,CONSTRAINT lk_deps_schema UNIQUE(name)
);

CREATE OR REPLACE FUNCTION deps.fv_schemas () 
	RETURNS SETOF deps.t_schema
AS $$
	SELECT
		ns.oid
		,ns.nspname
		,u.rolname
		,format('DROP SCHEMA IF EXISTS %I;', ns.nspname)
		,format('CREATE SCHEMA %I AUTHORIZATION %I;', ns.nspname, u.rolname)
	FROM pg_namespace ns 
	JOIN pg_roles u ON ns.nspowner = u.oid
	WHERE 
		ns.nspname NOT LIKE 'pg%' 
		AND ns.nspname NOT IN( 'information_schema'/*, 'deps'*/ )
		;
$$
LANGUAGE SQL
	STABLE
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- BASE TYPES

CREATE TABLE deps.t_base_type (
	oid 			oid NOT NULL
	,name 			text NOT NULL
	,owner			name NOT NULL
	,real_type_oid	oid 		-- FIXME references what ?
	
	,CONSTRAINT pk_deps_base_type PRIMARY KEY(oid)
);

CREATE OR REPLACE FUNCTION deps.hft_type_name(type_oid oid) 
	RETURNS TEXT
AS $$
	SELECT format_type(t.oid, t.typtypmod)
	FROM pg_type t
	WHERE t.oid = $1
	;
$$
LANGUAGE SQL 
	STABLE STRICT;	

CREATE OR REPLACE FUNCTION deps.fv_base_types () 
	RETURNS SETOF deps.t_base_type
AS $$
	SELECT
		t.oid
		,format_type(t.oid, t.typtypmod)
		,u.rolname
		,NULLIF(t.typrelid, 0)
	FROM pg_type t
	JOIN pg_namespace ns ON t.typnamespace = ns.oid
	JOIN pg_roles u ON t.typowner = u.oid
	;
$$
LANGUAGE SQL 
	STABLE
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- COMPOSITE TYPES

CREATE TABLE deps.t_composite_type (
	oid 			oid NOT NULL
	,schema_oid		oid NOT NULL
	,name 			name NOT NULL
	,fullname 		text NOT NULL
	,owner			name NOT NULL
	,object_kind	char(1) NOT NULL
	,sql_drop	text
	,sql_create	text
	
	,CONSTRAINT pk_deps_composite_type PRIMARY KEY(oid)
	,CONSTRAINT lk_deps_composite_type UNIQUE(fullname)
);

ALTER TABLE deps.t_composite_type
	ADD CONSTRAINT fk_deps_composite_type_to_schema 
		FOREIGN KEY(schema_oid)
		REFERENCES deps.t_schema(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE INDEX idx_deps_composite_type_kind ON deps.t_composite_type(object_kind);

CREATE OR REPLACE FUNCTION deps.hft_composite_type_fullname(type_oid oid) 
	RETURNS TEXT
AS $$
	SELECT 	format('%I.%I', n.nspname, c.relname)
	FROM 	pg_class c
	JOIN	pg_namespace n ON c.relnamespace = n.oid
	WHERE 	c.oid = $1
	;
$$
LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;	

CREATE OR REPLACE FUNCTION deps.fv_composite_types (schema_oid oid) 
	RETURNS SETOF deps.t_composite_type
AS $$
	SELECT
		x.*
		,format('DROP TYPE IF EXISTS %s;', x.fullname)
		,format(E'CREATE TYPE %s (\n\t%%s\n);', x.fullname)
	FROM (
		SELECT 
			c.oid
			,c.relnamespace
			,c.relname
			,deps.hft_composite_type_fullname(c.oid) AS fullname
			,u.rolname
			,'C'::char(1)
		FROM pg_class c
		JOIN pg_namespace ns ON c.relnamespace = ns.oid
		JOIN pg_roles u ON c.relowner = u.oid
		WHERE 
			c.relkind = 'c' 
			AND c.relnamespace = $1
		) x
	;
$$
LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;

CREATE OR REPLACE FUNCTION deps.fv_tables (schema_oid oid) 
	RETURNS SETOF deps.t_composite_type	
AS $$
	SELECT
		x.*
		,format('DROP TABLE IF EXISTS %s;', x.fullname)
		,format(E'CREATE TABLE %s (\n\t%%s\n);', x.fullname)
	FROM (
		SELECT 
			c.oid
			,c.relnamespace
			,c.relname
			,deps.hft_composite_type_fullname(c.oid) AS fullname
			,u.rolname
			,'T'::char(1)
		FROM pg_class c
		JOIN pg_namespace ns ON c.relnamespace = ns.oid
		JOIN pg_roles u ON c.relowner = u.oid
		WHERE c.relkind = 'r' 
			AND c.relnamespace = $1 
			AND c.relpersistence != 't'
		) x
	;
$$
LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;

CREATE OR REPLACE FUNCTION deps.fv_views (schema_oid oid) 
	RETURNS SETOF deps.t_composite_type	
AS $$
	SELECT
		x.*
		,format('DROP VIEW IF EXISTS %s;', x.fullname)
		,format(E'CREATE VIEW %s AS\n%s', x.fullname, pg_get_viewdef(x.oid, true))
	FROM (
		SELECT 
			c.oid
			,c.relnamespace
			,c.relname
			,deps.hft_composite_type_fullname(c.oid) AS fullname
			,u.rolname
			,'V'::char(1)
		FROM pg_class c
		JOIN pg_namespace ns ON c.relnamespace = ns.oid
		JOIN pg_roles u ON c.relowner = u.oid
		WHERE c.relkind = 'v' 
			AND c.relnamespace = $1
		) x
	;
$$
LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- COLUMNS

CREATE TABLE deps.t_column (
	composite_type_oid		oid NOT NULL
	,position				smallint NOT NULL
	,name 					name NOT NULL
	,type_oid				oid NOT NULL	-- FIXME references what ?
	,type_name				name NOT NULL
	,not_null				bool NOT NULL
	,default_value			text
	
	,CONSTRAINT pk_deps_column PRIMARY KEY(composite_type_oid, name)
);

ALTER TABLE deps.t_column
	ADD CONSTRAINT fk_deps_column_to_composite_type
		FOREIGN KEY(composite_type_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE OR REPLACE FUNCTION deps.fv_columns (composite_type_oid oid) 
	RETURNS SETOF deps.t_column
AS $$
	SELECT 
		a.attrelid
		,a.attnum
		,a.attname
		,a.atttypid
		,pg_catalog.format_type(a.atttypid, a.atttypmod)::name
		,a.attnotnull
		,d.adsrc
	FROM pg_attribute a
	LEFT JOIN pg_attrdef d ON a.attrelid = d.adrelid AND a.attnum = d.adnum
	WHERE 
		NOT a.attisdropped 
		AND a.attnum > 0 
		AND a.attrelid = $1
	;
$$
LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- PRIMARY KEYs / UNIQUEs

CREATE TABLE deps.t_pk_unique (
  oid 					oid NOT NULL
  ,composite_type_oid 	oid NOT NULL
  ,name 				name NOT NULL
  ,object_kind 			char(1) NOT NULL
  
  ,CONSTRAINT pk_deps_pk_unique PRIMARY KEY (oid)
  ,CONSTRAINT lk_deps_pk_unique UNIQUE (composite_type_oid , name)
  ,CONSTRAINT ck_deps_pk_unique_object_kind CHECK (object_kind IN('U','P'))
);

ALTER TABLE deps.t_pk_unique
	ADD CONSTRAINT fk_deps_pk_unique_to_composite_type
		FOREIGN KEY(composite_type_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

			
CREATE OR REPLACE FUNCTION deps.hft_pk_kind(character)
  RETURNS text 
AS $$
	SELECT CASE $1
		WHEN 'U' THEN 'UNIQUE'
		WHEN 'P' THEN 'PRIMARY KEY'
	END;
$$ LANGUAGE SQL 
	IMMUTABLE STRICT
	SECURITY DEFINER;

CREATE OR REPLACE FUNCTION deps.fv_pk_uniques(composite_type_oid oid) 
	RETURNS SETOF deps.t_pk_unique 
AS $$
	SELECT
		c.oid
		,c.conrelid
		,c.conname
		,UPPER(c.contype)
	FROM
		pg_constraint c
	WHERE
		c.conrelid = $1
		AND c.contype IN ('u','p');
$$ LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

CREATE TABLE deps.t_pk_unique_column (
  pk_unique_oid 		oid NOT NULL
  ,position 			smallint NOT NULL
  ,name 				name NOT NULL
  
  ,CONSTRAINT pk_deps_pk_unique_column PRIMARY KEY(pk_unique_oid, name)
);
ALTER TABLE deps.t_pk_unique_column
	ADD CONSTRAINT fk_deps_pk_unique_column_to_pk
		FOREIGN KEY(pk_unique_oid)
		REFERENCES deps.t_pk_unique(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE OR REPLACE FUNCTION deps.fv_pk_unique_columns(constraint_oid oid) 
	RETURNS SETOF deps.t_pk_unique_column 
AS $$
	SELECT
		c.oid,
		a.attnum,
		a.attname
	FROM	pg_constraint c 
	JOIN 	pg_attribute a on a.attrelid = c.conindid
	WHERE
		c.oid = $1
		AND a.attnum > 0;
$$ LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- FOREIGN KEYs

CREATE TABLE deps.t_foreign_key (
  oid 			oid NOT NULL
  ,master_oid 	oid NOT NULL
  ,foreign_oid 	oid NOT NULL
  ,name 			name NOT NULL
  ,delete_action char(1) NOT NULL
  ,update_action char(1) NOT NULL
  ,sql_create 	text NOT NULL
  ,sql_drop 	text NOT NULL
  
  ,CONSTRAINT pk_deps_foreign_key PRIMARY KEY (oid)
  ,CONSTRAINT lk_deps_foreign_key UNIQUE (master_oid, name)
  ,CONSTRAINT ck_deps_fk_delete_action CHECK( delete_action IN ('A', 'R', 'C', 'N', 'D'))
  ,CONSTRAINT ck_deps_fk_update_action CHECK( update_action IN ('A', 'R', 'C', 'N', 'D'))
);

ALTER TABLE deps.t_foreign_key
	ADD CONSTRAINT fk_deps_fk_to_master
		FOREIGN KEY(master_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;
ALTER TABLE deps.t_foreign_key
	ADD CONSTRAINT fk_deps_fk_to_foreign
		FOREIGN KEY(foreign_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;


CREATE OR REPLACE FUNCTION deps.fv_foreign_keys(composite_type_oid oid) 
	RETURNS SETOF deps.t_foreign_key
AS $$
	SELECT
		c.oid
		,c.conrelid
		,c.confrelid
		,c.conname
		,UPPER(c.confupdtype)
		,UPPER(c.confdeltype)
		,format(E'ALTER TABLE %I\n\tADD CONSTRAINT %s\n\tFOREIGN KEY( %%1s )\n\tREFERENCES %s( %%2s )\n\t\tON DELETE %s\n\t\tON UPDATE %s;', 
			deps.ft_object_fullname($1),
			c.conname, 
			deps.ft_object_fullname(c.confrelid), 
			deps.hft_fk_action(UPPER(c.confdeltype)),
			deps.hft_fk_action(UPPER(c.confupdtype))
		) || E'\n\t'
		,format('ALTER TABLE %I',deps.hft_composite_type_fullname($1)) || E'\n\t' || format('DROP CONSTRAINT %s;', c.conname)
	FROM
		pg_constraint c
	WHERE
		c.conrelid = $1
		AND c.contype = 'f';
$$ LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

	
CREATE OR REPLACE FUNCTION deps.hft_fk_action(character)
  RETURNS text 
AS $$
	SELECT CASE $1
		WHEN 'A' THEN 'NO ACTION'
		WHEN 'R' THEN 'RESTRICT'
		WHEN 'C' THEN 'CASCADE'
		WHEN 'N' THEN 'SET NULL'
		WHEN 'D' THEN 'SET DEFAULT'
	END;
$$ LANGUAGE SQL 
	IMMUTABLE STRICT
	SECURITY DEFINER;

CREATE TABLE deps.t_foreign_key_column (
  fk_oid 		oid NOT NULL
  ,fk_table_oid	oid NOT NULL	-- could not be constrainted !
  ,position 	smallint NOT NULL
  ,name 		name NOT NULL
  
  ,CONSTRAINT pk_deps_fk_column PRIMARY KEY(fk_oid, fk_table_oid, name)
);

CREATE INDEX idx_deps_fk_column_table ON deps.t_foreign_key_column(fk_table_oid);

ALTER TABLE deps.t_foreign_key_column
	ADD CONSTRAINT fk_deps_fk_column_to_fk
		FOREIGN KEY(fk_oid)
		REFERENCES deps.t_foreign_key(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE OR REPLACE FUNCTION deps.fv_foreign_key_columns(constraint_oid oid) 
	RETURNS SETOF deps.t_foreign_key_column 
AS $$
	SELECT *
	FROM (
		SELECT
			c.oid
			,a.attrelid
			,a.attnum
			,a.attname
		FROM 	pg_constraint c 
		JOIN  	pg_attribute a 
			ON 	a.attrelid = c.conrelid
				AND a.attnum = ANY (c.conkey)
		WHERE	c.oid = $1
		
		UNION ALL

		SELECT
			c.oid
			,a.attrelid
			,a.attnum
			,a.attname		
		FROM 	pg_constraint c 
		JOIN  	pg_attribute a 
			ON 	a.attrelid = c.confrelid
				AND a.attnum = ANY (c.confkey)
		WHERE	c.oid = $1
	) AS q
$$ LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- CHECKs
CREATE TABLE deps.t_check(
  oid 					oid NOT NULL
  ,composite_type_oid 	oid NOT NULL
  ,name 				name NOT NULL
  ,condition 			text NOT NULL
  ,CONSTRAINT pk_deps_check PRIMARY KEY (oid)
  ,CONSTRAINT lk_deps_check UNIQUE (composite_type_oid, name)
);

ALTER TABLE deps.t_check
	ADD CONSTRAINT fk_deps_check_to_composite_type
		FOREIGN KEY(composite_type_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;


CREATE OR REPLACE FUNCTION deps.fv_checks(composite_type_oid oid)
	RETURNS SETOF deps.t_check
AS $$
	SELECT
		c.oid
		,c.conrelid
		,c.conname
		,format('(%s)',c.consrc)
	FROM pg_constraint c
	WHERE c.conrelid = $1
		AND c.contype = 'c';
$$
  LANGUAGE sql VOLATILE;

-- ----------------------------------------------------------------------------------
-- RULES

CREATE TABLE deps.t_rule (
	oid 					oid NOT NULL
	,composite_type_oid		oid NOT NULL
	,name 					name NOT NULL
	,sql_drop	text
	,sql_create	text
	
	,CONSTRAINT pk_deps_rule PRIMARY KEY(oid)
	,CONSTRAINT lk_deps_rule UNIQUE(composite_type_oid, name)
);

ALTER TABLE deps.t_rule
	ADD CONSTRAINT fk_deps_rule_to_composite_type
		FOREIGN KEY(composite_type_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;


CREATE OR REPLACE FUNCTION deps.fv_rules (composite_type_oid oid) RETURNS SETOF deps.t_rule	
AS $$
	SELECT DISTINCT
		r.oid
		,t.oid
		,r.rulename
		,format('DROP RULE IF EXISTS %I ON %I.%I;', r.rulename, ns.nspname, t.relname)
		,pg_get_ruledef(r.oid, true)
	FROM pg_class t
	JOIN pg_namespace ns ON t.relnamespace = ns.oid
	JOIN pg_rewrite r ON r.ev_class = t.oid
	WHERE 
		r.rulename != '_RETURN'
		AND t.oid = $1
	;
$$
LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- FUNCTIONS

CREATE TABLE deps.t_function (
	oid 		oid NOT NULL
	,schema_oid	oid NOT NULL
	,name 		name NOT NULL
	,fullname 	text NOT NULL
	,owner		name NOT NULL
	,used_type_oids	oid[]
	,signature	text NOT NULL
	,sql_drop	text
	,sql_create	text
	
	,CONSTRAINT pk_deps_function PRIMARY KEY(oid)
	,CONSTRAINT lk_deps_function UNIQUE(signature)
);

CREATE INDEX idx_deps_function_fullname ON deps.t_function(fullname);

ALTER TABLE deps.t_function
	ADD CONSTRAINT fk_deps_function_to_schema 
		FOREIGN KEY(schema_oid)
		REFERENCES deps.t_schema(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE OR REPLACE FUNCTION deps.hft_function_fullname(func_oid oid) RETURNS TEXT
AS $$
	SELECT format('%I.%I', ns.nspname, p.proname) 
	FROM pg_proc p 
	JOIN pg_namespace ns ON p.pronamespace = ns.oid 
	WHERE p.oid = $1
	;
$$
LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

CREATE OR REPLACE FUNCTION deps.fv_functions (schema_oid oid) RETURNS SETOF deps.t_function	
AS $$
	SELECT
		x.oid
		,x.pronamespace
		,x.proname
		,x.fullname
		,x.rolname
		,x.used_type_oids
		,format('%s(%s)', x.fullname, x.args)
		,format('DROP FUNCTION IF EXISTS %s(%s);', x.fullname, x.args)
		,pg_get_functiondef(x.oid)
	FROM (
		SELECT 
			p.oid
			,p.pronamespace
			,p.proname
			,deps.hft_function_fullname(p.oid) AS fullname
			,u.rolname
			,ARRAY[]::oid[] || p.prorettype || string_to_array(p.proargtypes::text, ' ')::oid[] || p.proallargtypes AS used_type_oids
			,pg_get_function_identity_arguments(p.oid) AS args
		FROM pg_proc p
		JOIN pg_namespace ns ON p.pronamespace = ns.oid
		JOIN pg_roles u ON p.proowner = u.oid
		JOIN pg_type rt ON p.prorettype = rt.oid
		WHERE p.pronamespace = $1 
			AND NOT p.proisagg
		) x
	;
$$
LANGUAGE SQL 
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- OPERATORS

CREATE OR REPLACE FUNCTION deps.hft_operator_fullname(opr_oid oid) RETURNS TEXT
AS $$
	SELECT format('%I.%I', ns.nspname, p.oprname) 
	FROM pg_operator p 
	JOIN pg_namespace ns ON p.oprnamespace = ns.oid 
	WHERE p.oid = $1
	;
$$
LANGUAGE SQL
	STABLE STRICT;

CREATE TABLE deps.t_operator (
	oid 			oid NOT NULL
	,schema_oid		oid NOT NULL
	,name 			name NOT NULL
	,fullname 		text NOT NULL
	,owner			name NOT NULL
	,func_oid		oid NOT NULL
	,ret_oid		oid NOT NULL
	,left_oid		oid
	,right_oid		oid
	,comm_oid		oid
	,neg_oid		oid
	,can_hash	bool NOT NULL
	,can_merge	bool NOT NULL
	,est_rest	text
	,est_join	text
	,sql_drop	text
	,sql_create	text
	
	,CONSTRAINT pk_deps_operator PRIMARY KEY(oid)
);

ALTER TABLE deps.t_operator
	ADD CONSTRAINT fk_deps_operator_to_schema 
		FOREIGN KEY(schema_oid)
		REFERENCES deps.t_schema(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

CREATE INDEX idx_deps_operator_fullname ON deps.t_operator(fullname);

CREATE OR REPLACE FUNCTION deps.fv_operators (schema_oid oid) RETURNS SETOF deps.t_operator
AS $$
	SELECT
		op.oid
		,op.oprnamespace
		,op.oprname
		,deps.hft_operator_fullname(op.oid)
		,u.rolname
		,op.oprcode
		,op.oprresult
		,NULLIF(op.oprleft, 0)
		,NULLIF(op.oprright, 0)
		,NULLIF(op.oprcom, 0)
		,NULLIF(op.oprnegate, 0)
		,op.oprcanhash
		,op.oprcanmerge
		,deps.hft_function_fullname(NULLIF(op.oprrest, 0))
		,deps.hft_function_fullname(NULLIF(op.oprjoin, 0))
		,format('DROP OPERATOR IF EXISTS %I.%I(%s,%s)', ns.nspname, op.oprname, 
			COALESCE(deps.hft_type_name(NULLIF(op.oprleft, 0)), 'NONE'),
			COALESCE(deps.hft_type_name(NULLIF(op.oprright, 0)), 'NONE'))
		,format('CREATE OPERATOR %I.%I(PROCEDURE = %s', ns.nspname, op.oprname, deps.hft_function_fullname(op.oprcode))
			|| CASE WHEN NULLIF(op.oprleft, 0) IS NULL THEN '' ELSE format(', LEFTARG = %s', deps.hft_type_name(op.oprleft)) END
			|| CASE WHEN NULLIF(op.oprright, 0) IS NULL THEN '' ELSE format(', RIGHTARG = %s', deps.hft_type_name(op.oprright)) END
			|| CASE WHEN NULLIF(op.oprcom, 0) IS NULL THEN '' ELSE format(', COMMUTATOR = %s', deps.hft_operator_fullname(op.oprcom)) END
			|| CASE WHEN NULLIF(op.oprnegate, 0) IS NULL THEN '' ELSE format(', NEGATOR = %s', deps.hft_operator_fullname(op.oprnegate)) END
			|| CASE WHEN NULLIF(op.oprrest, 0) IS NULL THEN '' ELSE format(', RESTRICT = %s', deps.hft_function_fullname(op.oprrest)) END
			|| CASE WHEN NULLIF(op.oprjoin, 0) IS NULL THEN '' ELSE format(', JOIN = %s', deps.hft_function_fullname(op.oprjoin)) END
			|| deps.fa_iif(op.oprcanhash, ', HASHES', ''::text)
			|| deps.fa_iif(op.oprcanmerge, ', MERGES', ''::text)
			|| ')'
	FROM pg_operator op
	JOIN pg_namespace ns ON op.oprnamespace = ns.oid
	JOIN pg_roles u ON op.oprowner = u.oid
	--JOIN pg_proc p ON op.oprcode = p.oid
	WHERE op.oprnamespace = $1;
$$
LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;

-- ----------------------------------------------------------------------------------
-- TRIGGERS

CREATE TABLE deps.t_trigger (
	oid 				oid NOT NULL
	,composite_type_oid	oid NOT NULL
	,name 				name NOT NULL
	,function_oid		oid NOT NULL
	,sql_drop		text
	,sql_create		text
	
	,CONSTRAINT pk_deps_trigger PRIMARY KEY(oid)
	,CONSTRAINT lk_deps_trigger UNIQUE(composite_type_oid, name)
);

ALTER TABLE deps.t_trigger
	ADD CONSTRAINT fk_deps_trigger_to_composite_type
		FOREIGN KEY(composite_type_oid)
		REFERENCES deps.t_composite_type(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;

ALTER TABLE deps.t_trigger
	ADD CONSTRAINT fk_deps_trigger_to_function
		FOREIGN KEY(function_oid)
		REFERENCES deps.t_function(oid) 
			ON DELETE CASCADE
			ON UPDATE NO ACTION;


CREATE OR REPLACE FUNCTION deps.fv_triggers(composite_type_oid oid) RETURNS SETOF deps.t_trigger
AS $$
	SELECT 
		t.oid
		,t.tgrelid
		,t.tgname 
		,t.tgfoid
		,format('DROP TRIGGER IF EXISTS %I ON %s;', tgname, deps.hft_composite_type_fullname(t.tgrelid))
		,pg_get_triggerdef(oid) || ';'
	FROM pg_trigger t
	WHERE NOT t.tgisinternal
		AND t.tgrelid = $1
	;
$$
LANGUAGE SQL
	STABLE STRICT
	SECURITY DEFINER;
