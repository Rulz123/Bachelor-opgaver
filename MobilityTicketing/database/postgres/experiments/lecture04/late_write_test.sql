\set ON_ERROR_STOP on

\echo INSERT FROM OLD APPLICATION
\ir late_old_writer.sql

\echo BEFORE BACKFILL
select id, product_code, product_id
from tickets
where id = 'LAB04-OLD-LATE';

\echo CATCH UP THE LATE TICKET
\ir ../../migrations/lecture04/031_backfill_ticket_product.sql

\echo AFTER BACKFILL
select id, product_code, product_id
from tickets
where id = 'LAB04-OLD-LATE';

\echo CONFIRM NOTHING ELSE REMAINS
\ir ../../migrations/lecture04/031_backfill_ticket_product.sql
