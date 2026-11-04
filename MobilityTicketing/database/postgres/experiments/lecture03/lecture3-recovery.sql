\set ON_ERROR_STOP on

\echo BEFORE DUPLICATE
\ir compare_revenue.sql

-- Allow this expected error so we can record its SQLSTATE.
\set ON_ERROR_STOP off
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-DUPLICATE', 'USER-1', 'TICKET-1',
    'gateway-capture-0001', 36, 'DKK', 'Captured',
    '2026-04-29 10:10:00+00'
);
\echo Duplicate attempt SQLSTATE: :SQLSTATE -- expected 23505
\set ON_ERROR_STOP on

\echo AFTER DUPLICATE ATTEMPT
\ir compare_revenue.sql

refresh materialized view daily_captured_revenue;

\echo AFTER MATERIALIZED VIEW REFRESH
\ir compare_revenue.sql

-- Rebuild this operator/day from the authoritative base data.
-- Lab assumption: no concurrent payment writes during repair.
insert into daily_revenue_by_operator (
    operator_id, revenue_date, captured_amount, captured_payments
)
select 'OP-METRO', date '2026-04-29',
       captured_amount, captured_payments
from captured_revenue_for_day('OP-METRO', date '2026-04-29')
on conflict (operator_id, revenue_date)
do update set
    captured_amount = excluded.captured_amount,
    captured_payments = excluded.captured_payments;

\echo AFTER TRIGGER SUMMARY REBUILD
\ir compare_revenue.sql