\set ON_ERROR_STOP on

begin;

\echo OLD WRITER AFTER ID BECOMES REQUIRED
\set ON_ERROR_STOP off
insert into tickets (
    id, user_id, trip_id, ticket_code, status, product_code,
    valid_from_utc, valid_to_utc, price, currency
)
select
    'LAB04-OLD-BLOCKED', user_id, trip_id,
    'LAB04-CODE-OLD-BLOCKED', status, product_code,
    valid_from_utc, valid_to_utc, price, currency
from tickets
where id = 'TICKET-1';

\echo Actual SQLSTATE: :SQLSTATE -- expected 23502
\set ON_ERROR_STOP on

rollback;

\echo OLD READER STILL WORKS
\ir old_reader.sql