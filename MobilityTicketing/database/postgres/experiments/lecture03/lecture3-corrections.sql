\set ON_ERROR_STOP on

\echo BEFORE CORRECTIONS
\ir compare_revenue.sql

-- Previously failed payment is now captured.
update payments
set status = 'Captured'
where id = 'PAY-CASE-FAILED';

\echo AFTER FAILED TO CAPTURED
\ir compare_revenue.sql

-- Previously captured payment is now refunded.
update payments
set status = 'Refunded'
where id = 'PAY-CASE-CAPTURED';

\echo AFTER CAPTURED TO REFUNDED
\ir compare_revenue.sql

-- Delete only the payment created for this experiment.
delete from payments
where id = 'PAY-CASE-FAILED';

\echo AFTER TEST PAYMENT DELETION
\ir compare_revenue.sql