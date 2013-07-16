CREATE OR REPLACE FUNCTION deps.fi_object_oid(fullname TEXT) RETURNS oidAS $$	SELECT	COALESCE(		(SELECT oid FROM deps.t_composite_type WHERE fullname = $1)		,(SELECT oid FROM deps.t_function WHERE fullname = $1)		,(SELECT oid FROM deps.t_operator WHERE fullname = $1)	);$$LANGUAGE SQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.fi_object_oid(TEXT) IS 'Ritorna l''oid dell''oggetto con il nome completo (schema.nome_oggetto) indicato';	CREATE OR REPLACE FUNCTION deps.hfv_cascades_to(object_oid oid) RETURNS SETOF oidAS $$DECLARE	_dep oid;BEGIN	FOR	_dep	IN	SELECT 	oid_cascades_to		FROM	deps.t_deps		WHERE	oid_master = $1	LOOP		RETURN NEXT _dep;		RETURN QUERY			SELECT	*			FROM	deps.hfv_cascades_to(_dep);	END LOOP;		RETURN;END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;CREATE OR REPLACE FUNCTION deps.hfv_cascades_from(object_oid oid) RETURNS SETOF oidAS $$DECLARE	_dep oid;BEGIN	FOR	_dep	IN	SELECT 	oid_master		FROM	deps.t_deps		WHERE	oid_cascades_to = $1	LOOP		RETURN NEXT _dep;		RETURN QUERY			SELECT	*			FROM	deps.hfv_cascades_from(_dep);	END LOOP;		RETURN;END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;	CREATE OR REPLACE FUNCTION deps.ft_object_fullname(object_oid oid)RETURNS TEXT AS$$	SELECT format('%I.%I', deps.hft_namespace($1), deps.hft_name($1));$$LANGUAGE SQL 	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_object_fullname(oid) IS 'Ritorna il nome completo (schema.nome_oggetto) dell''oggetto con l''oid indicato';CREATE OR REPLACE FUNCTION deps.ft_sql_create(object_oid oid) RETURNS TEXTAS $$	SELECT	COALESCE(		(SELECT sql_create FROM deps.t_composite_type WHERE oid = $1)		,(SELECT sql_create FROM deps.t_function WHERE oid = $1)		,(SELECT sql_create FROM deps.t_operator WHERE oid = $1)		,(SELECT sql_create FROM deps.t_trigger WHERE oid = $1)	);$$LANGUAGE SQL 	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_create(oid) IS 'Ritorna il codice SQL di creazione del singolo oggetto indicato (senza cascade)';CREATE OR REPLACE FUNCTION deps.ft_sql_drop(object_oid oid) RETURNS TEXTAS $$	SELECT	COALESCE(		(SELECT sql_drop FROM deps.t_composite_type WHERE oid = $1)		,(SELECT sql_drop FROM deps.t_function WHERE oid = $1)		,(SELECT sql_drop FROM deps.t_operator WHERE oid = $1)		,(SELECT sql_drop FROM deps.t_trigger WHERE oid = $1)	);$$LANGUAGE SQL 	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_drop(oid) 	IS 'Ritorna il codice SQL di eliminazione del singolo oggetto indicato (senza cascade)';-- ------------------------------------------------------------------------------------ DELETE CHAINCREATE OR REPLACE FUNCTION deps.hfa_sql_drop_chain(object_oid oid, INOUT visited oid[], INOUT cascades TEXT[])AS $$DECLARE	_slave oid;	_sql TEXT;BEGIN	BEGIN	cascades := cascades || ('-- deps of '::text || $1::text || ' = '::text || deps.ft_object_fullname($1));		FOR	_slave																	-- prende uno ad uno gli oggetti della query di sotto	IN	SELECT	d.oid_cascades_to												--{		FROM	deps.t_deps d													--		seleziona tutti gli oggetti che dipendono dal oggetto passato come parametro		WHERE	d.oid_master = $1												--		senza includere quelli gia visti nelle ricorsioni precedenti			AND d.oid_cascades_to != ALL(visited)								--		ORDER BY	1															--}	LOOP		IF _slave != ALL(visited) THEN			visited := visited || _slave;							-- aggiunge al array l'oggetto selezionato dal FOR			SELECT *			FROM deps.hfa_sql_drop_chain(_slave, visited, cascades)	-- ricorsione per riempire gli array visited e cascades			INTO visited, cascades;		END IF;	END LOOP;		_sql := deps.ft_sql_drop($1);												-- scrive il codice SQL di eliminazione dell oggetto indicato (l'ultimo valore dell'array visited)	IF (_sql IS NULL) THEN		RAISE NOTICE 'No DROP command for oid %', $1;							-- se non ci sono piu oggetti da eliminare per l'oggetto indicato, compare questa notizia	ELSE		cascades := cascades || _sql;											-- aggiunge all'array cascades il codice di eliminazione memorizzato in _sql	END IF;	cascades := cascades || ('-- end of deps of '::text || $1::text || ' = '::text || deps.ft_object_fullname($1));   -- aggiunge all'array cascades il messaggio che avvisa che � gia stato eliminato l'oggetto	EXCEPTION WHEN null_value_not_allowed THEN	END;END $$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.hfa_sql_drop_chain(object_oid oid, INOUT visited oid[], INOUT cascades TEXT[]) 	IS 'Concatena in _cascades il codice di drop di un elemento e delle sue dipendenze';CREATE OR REPLACE FUNCTION deps.ft_sql_drop_chain(replacement oid[]) RETURNS TEXTAS $$DECLARE	_oid oid;	_cascades TEXT[] = ARRAY[]::TEXT[];	_visited oid[] = ARRAY[]::oid[];BEGIN	_cascades := _cascades || '-- SQL DROP:'::text;	FOREACH	_oid IN	ARRAY replacement	LOOP		IF _oid != ALL(_visited) THEN			SELECT *			FROM deps.hfa_sql_drop_chain(_oid, _visited, _cascades)	-- ricorsione per riempire gli array _visited e _cascades			INTO _visited, _cascades;		END IF;	END LOOP;	_cascades := _cascades || '-- END DROP'::text;		RETURN array_to_string(_cascades, E'\n');END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_drop_chain(oid) IS 'Ritorna il codice SQL di eliminazione a cascata per gli oggetti indicati';	CREATE OR REPLACE FUNCTION deps.ft_sql_drop_chain(object_oid oid) RETURNS TEXTAS $$	SELECT array_to_string(cascades, E'\n') FROM deps.hfa_sql_drop_chain($1, ARRAY[]::oid[], ARRAY[]::TEXT[]) AS x(y, cascades);$$LANGUAGE SQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_drop_chain(oid) IS 'Ritorna il codice SQL di eliminazione a cascata per l''oggetto indicato';-- ------------------------------------------------------------------------------------ CREATE CHAINCREATE OR REPLACE FUNCTION deps.hfa_sql_create_chain(object_oid oid, replacement hstore, INOUT visited oid[], INOUT cascades text[])AS $$DECLARE	_slave oid;	_sql TEXT;BEGIN	BEGIN		_sql := COALESCE($2 -> $1::text, deps.ft_sql_create($1));	-- memorizza in _sql il codice SQL di creazione dell'oggetto indicato, l'operatore "->" � definito sulla base del hstore (hash map)		IF (_sql IS NULL) THEN			RAISE NOTICE 'No CREATE command for oid %', $1;		ELSE			cascades := cascades				|| ('-- deps of '::text || $1::text || ' = '::text || deps.ft_object_fullname($1))				|| _sql;							-- aggiunge alla variabile _cascades il codice SQL memorizzato nella variabile _sql		END IF;				FOR	_slave											--	prende uno ad uno i valori della query di sotto		IN	SELECT	d.oid_cascades_to						--{			FROM	deps.t_deps d							--		seleziona tutti gli oggetti che dipendono dal oggetto passato come parametro			WHERE	d.oid_master = $1						--		senza includere quelli gia visti nelle ricorsioni precedenti				AND d.oid_cascades_to != ALL(visited)		--			ORDER BY	1									--}		LOOP			IF _slave != ALL (visited) THEN				visited := visited || _slave;				SELECT *				FROM deps.hfa_sql_create_chain(_slave, $2, visited, cascades)	-- aggiunge alla variabile _cascades il risultato delle ricorsioni				INTO visited, cascades;			END IF;		END LOOP;		cascades := cascades			|| ('-- end of deps of '::text || $1::text || ' = '::text || deps.ft_object_fullname($1));	EXCEPTION WHEN null_value_not_allowed THEN	END;END $$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.hfa_sql_create_chain(object_oid oid, replacements hstore, INOUT visited oid[], INOUT cascades text[]) 	IS 'Funzione incaricata di restituire il codice sql di creazione degli elementi fino all''oid indicato';-- codice sql di creazione a cascata degli elementi contenuti nell'arrayCREATE OR REPLACE FUNCTION deps.ft_sql_create_chain(oidElem oid[]) RETURNS TEXTAS $$DECLARE	_oid oid;	_cascades TEXT[] = ARRAY[]::TEXT[];	_visited oid[] = ARRAY[]::oid[];BEGIN	_cascades := _cascades || '-- SQL CREATE:'::text;	FOREACH	_oid IN	ARRAY oidElem	LOOP		IF _oid != ALL(_visited) THEN			SELECT *			FROM deps.hfa_sql_create_chain(_oid, ''::hstore, _visited, _cascades)	-- ricorsione per riempire gli array _visited e _cascades			INTO _visited, _cascades;		END IF;	END LOOP;	_cascades := _cascades || '-- END CREATE'::text;		RETURN array_to_string(_cascades, E'\n');END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_drop_chain(oid) IS 'Ritorna il codice SQL di creazione a cascata per gli oggetti indicati';-- normal create chain of an objectCREATE OR REPLACE FUNCTION deps.ft_sql_create_chain(object_oid oid, replacements hstore = ''::hstore) RETURNS TEXTAS $$	SELECT array_to_string(cascades, E'\n\n') FROM deps.hfa_sql_create_chain($1, $2, ARRAY[]::oid[], ARRAY[]::TEXT[]) AS x(y, cascades);$$LANGUAGE SQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_create_chain(oid, hstore) IS 'Ritorna il codice SQL di creazione a cascata per l''oggetto indicato, tenendo conto di eventuali aggiornamenti ad altri oggetti';-- ------------------------------------------------------------------------------------ UPDATE OBJECT-- create chain of object dependantsCREATE OR REPLACE FUNCTION deps.hfa_sql_create_dependants(object_oid oid, replacements hstore, INOUT cascades TEXT[], INOUT visited oid[])AS $$DECLARE	_cascades TEXT[] = ARRAY[]::TEXT[];	_slave oid;BEGIN	FOR	_slave	IN	SELECT	d.oid_cascades_to		FROM	deps.t_deps d		WHERE	d.oid_master = $1			AND d.oid_cascades_to != ALL(visited)		ORDER BY	1	LOOP		IF _slave != ALL (visited) THEN			visited := visited || _slave;			SELECT *			FROM deps.hfa_sql_create_chain(_slave, $2, visited, _cascades)	-- aggiunge alla variabile _cascades il risultato delle ricorsioni			INTO visited, _cascades;					END IF;	END LOOP;	cascades := cascades || _cascades;END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;CREATE OR REPLACE FUNCTION deps.hfa_sql_create_dependants(object_oid oid, replacements hstore) RETURNS TEXT[]AS $$DECLARE	_cascades TEXT[] = ARRAY[]::TEXT[];	_slave oid;	_visited oid[] DEFAULT ARRAY[object_oid]::oid[];BEGIN	FOR	_slave	IN	SELECT	d.oid_cascades_to		FROM	deps.t_deps d		WHERE	d.oid_master = $1			AND d.oid_cascades_to != ALL(_visited)		ORDER BY	1	LOOP		IF _slave != ALL (_visited) THEN			_visited := _visited || _slave;			SELECT *			FROM deps.hfa_sql_create_chain(_slave, $2, _visited, _cascades)	-- aggiunge alla variabile _cascades il risultato delle ricorsioni			INTO _visited, _cascades;					END IF;	END LOOP;	-- FIXME	RETURN _cascades;	--RETURN ARRAY[array_to_string(_cascades, E'\n\n')];END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;-- create chain with object updateCREATE OR REPLACE FUNCTION deps.ft_sql_update(replacements hstore) RETURNS TEXTAS $$DECLARE	_oid oid;	_cascades TEXT[] = ARRAY[]::TEXT[];	_cascadesaux TEXT[] = ARRAY[]::TEXT[];	_visited oid[] = ARRAY[]::oid[];BEGIN	_cascades := _cascades || '-- SQL CREATE:'::text;	FOR	_oid				--{	IN	SELECT	*			--	seleziona uno ad uno gli oid dell'hstore		FROM	skeys($1)	--		ORDER BY 	1		--}	LOOP		IF _oid != ALL(_visited) THEN			BEGIN				_visited := _visited || _oid;								SELECT *				FROM deps.hfa_sql_create_dependants(_oid, $1, _cascadesaux, _visited)				INTO _cascadesaux, _visited;								_cascades := _cascades						|| format('-- deps of %s = %s', _oid, deps.ft_object_fullname(_oid))  			-- inizia il codice di creazione dichiarando l'inizio delle dipendenze del primo elemento del hstore						|| COALESCE($1 -> _oid::text, deps.ft_sql_create(_oid))						|| _cascadesaux						|| format('-- End of deps of %s = %s', _oid, deps.ft_object_fullname(_oid));	-- fine del codice di creazione per l'elemento in considerazione e delle sue dipendenze se c'erano						EXCEPTION WHEN null_value_not_allowed THEN			END;		END IF;	END LOOP;	_cascades := _cascades || '-- END CREATE'::text;	RETURN array_to_string(_cascades, E'\n\n');END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_update(hstore) 	IS 'Funzione incaricata di restituire ed aggiornare il codice sql degli elementi di un hstore nel quale � memorizzato i loro nuovi codici sql di creazione e delle loro dipendenze';CREATE OR REPLACE FUNCTION deps.ft_sql_update(object_oid oid, new_sql TEXT) RETURNS TEXTAS $$DECLARE	_cascades TEXT[] = ARRAY[]::TEXT[];	_visited oid[] = ARRAY[]::oid[];BEGIN	_cascades := _cascades				|| '-- SQL CREATE:'::text				|| format('-- deps of %s = %s', $1, deps.ft_object_fullname($1))  		-- inizia il codice di creazione dichiarando l'inizio delle dipendenze dell'elemento passato come parametro				|| new_sql				|| deps.hfa_sql_create_dependants($1, ''::hstore)				|| format('-- End of deps of %s = %s', $1, deps.ft_object_fullname($1))	-- fine del codice di creazione per l'elemento in considerazione e delle sue dipendenze se c'erano				|| '-- END CREATE'::text;	RETURN array_to_string(_cascades, E'\n\n');END$$LANGUAGE PLPGSQL	STABLE STRICT	SECURITY DEFINER;COMMENT ON FUNCTION deps.ft_sql_update(oid, text) IS 'Ritorna il codice SQL di creazione a cascata per l''oggetto indicato, con una nuova definizione per esso';SELECT pr_change_owner('deps', 'wheel');SELECT pr_grant_usage('deps', 'dev');SELECT pr_grant_usage('deps', 'user');