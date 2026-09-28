\set ON_ERROR_STOP on

\echo BASELINE
\ir compare_revenue.sql

insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-CAPTURED', 'USER-1', 'TICKET-1',
    'gateway-case-captured', 36, 'DKK', 'Captured',
    '2026-04-29 10:00:00+00'
);

\echo AFTER CAPTURED INSERT
\ir compare_revenue.sql

insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-FAILED', 'USER-1', 'TICKET-1',
    'gateway-case-failed', 50, 'DKK', 'Failed',
    '2026-04-29 10:05:00+00'
);

\echo AFTER FAILED INSERT
\ir compare_revenue.sql