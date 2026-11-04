\set ON_ERROR_STOP on

begin;

alter table tickets drop column product_code;
alter table tickets add column product_id uuid;

\echo NEW COLUMN DOES NOT KNOW THE ORIGINAL PRODUCT
select id, product_id, price, currency
from tickets
order by id;

\echo OLD READER NOW FAILS
\set ON_ERROR_STOP off
select id, product_code, price, currency
from tickets
order by id;
\echo Old-reader SQLSTATE: :SQLSTATE -- expected 42703
\set ON_ERROR_STOP on

rollback;

\echo ORIGINAL DATA RESTORED
select id, product_code, price, currency
from tickets
order by id;