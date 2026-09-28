\set ON_ERROR_STOP on

\echo PUBLIC VIEWS MENTIONING PRODUCT_CODE
select viewname, definition
from pg_views
where schemaname = 'public'
  and definition ilike '%product_code%';

\echo PUBLIC MATERIALIZED VIEWS MENTIONING PRODUCT_CODE
select matviewname, definition
from pg_matviews
where schemaname = 'public'
  and definition ilike '%product_code%';

\echo PUBLIC FUNCTIONS MENTIONING PRODUCT_CODE
select p.proname, pg_get_functiondef(p.oid)
from pg_proc p
join pg_namespace n on n.oid = p.pronamespace
where n.nspname = 'public'
  and p.prokind = 'f'
  and p.prosrc ilike '%product_code%';

-- Select the DAY product ID as the caller's input.
select id as product_id
from products
where code = 'DAY'
\gset

\set ticket_id 'LAB04-FINAL-1'
\set ticket_code 'LAB04-CODE-FINAL-1'
\set agreed_price 65
\set agreed_currency 'DKK'

begin;
set local lock_timeout = '3s';

\echo REMOVE OLD REFERENCE WITHOUT CASCADE
alter table tickets drop column product_code;

\echo TEST ID-ONLY WRITER
\ir final_writer.sql

\echo TEST ID-ONLY READER
\ir final_reader.sql

rollback;

\echo AFTER ROLLBACK -- ORIGINAL SIX TICKETS
\ir final_reader.sql

\echo OLD COLUMN AND ORIGINAL VALUES RESTORED
\ir verify.sql