\set ON_ERROR_STOP on

\echo FIRST BACKFILL
\ir ../../migrations/lecture04/031_backfill_ticket_product.sql

\echo SECOND BACKFILL
\ir ../../migrations/lecture04/031_backfill_ticket_product.sql

\echo TICKETS AFTER BACKFILL
\ir new_reader.sql