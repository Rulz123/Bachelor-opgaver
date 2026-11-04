\set ON_ERROR_STOP on

\echo CHECK CURRENT DATA
\ir verify.sql

begin;

update tickets
set product_code = 'SINGLE'
where id = 'LAB04-NEW-1';

\echo DELIBERATE MISMATCH -- expect LAB04-NEW-1 in reference problems
\ir verify.sql

rollback;

\echo CHECK RESTORED DATA
\ir verify.sql