\set ON_ERROR_STOP on

select id as product_id
from products
where code = 'DAY'
\gset

\set ticket_id 'LAB04-CONFLICT-INPUT'
\set ticket_code 'LAB04-CODE-CONFLICT-INPUT'
\set agreed_price 65
\set agreed_currency 'DKK'

-- Simulate an extra, conflicting caller input.
-- Our writer intentionally does not use this variable.
\set supplied_product_code 'SINGLE'
\echo Caller supplied code: :supplied_product_code
\echo Expected stored code: DAY, derived from the product ID

begin;
\ir new_writer.sql
rollback;