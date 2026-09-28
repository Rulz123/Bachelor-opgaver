\set ON_ERROR_STOP on

begin;

-- Simulate one ticket that has not been backfilled.
update tickets
set product_id = null
where id = 'LAB04-OLD-LATE';

\echo REQUIRE ID TOO EARLY -- expect SQLSTATE 23502
\set ON_ERROR_STOP off
alter table tickets
    alter column product_id set not null;
\echo Actual SQLSTATE: :SQLSTATE
\set ON_ERROR_STOP on

rollback;

\echo ORIGINAL VALID DATA RESTORED
\ir verify.sql