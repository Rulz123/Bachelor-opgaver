-- Run this script after applying all four reporting mechanisms.
-- Start from a clean container when repeating the complete sequence.

set timezone = 'UTC';

begin;
delete from payments where id like 'PAY-CASE-%';
truncate daily_revenue_by_operator;
refresh materialized view daily_captured_revenue;

create function pg_temp.check_revenue(label text, expected_live numeric, expected_summary numeric, expected_view numeric)
returns void language plpgsql as $$
declare live_amount numeric; function_amount numeric; summary_amount numeric; view_amount numeric;
begin
        select coalesce(sum(payment.amount), 0) into live_amount
        from payments payment
        join tickets ticket on ticket.id = payment.ticket_id
        join trips trip on trip.id = ticket.trip_id
        join routes route on route.id = trip.route_id
        where route.operator_id = 'OP-METRO' and payment.created_utc::date = date '2026-04-29'
            and payment.status = 'Captured';
        select captured_amount into function_amount from captured_revenue_for_day('OP-METRO', date '2026-04-29');
        select coalesce(sum(captured_amount), 0) into summary_amount from daily_revenue_by_operator
        where operator_id = 'OP-METRO' and revenue_date = date '2026-04-29';
        select captured_amount into view_amount from daily_captured_revenue
        where operator_id = 'OP-METRO' and revenue_date = date '2026-04-29';
        if live_amount is distinct from expected_live or function_amount is distinct from expected_live
             or summary_amount is distinct from expected_summary or view_amount is distinct from expected_view then
                raise exception '%: direct=%, function=%, summary=%, view=%', label, live_amount, function_amount, summary_amount, view_amount;
        end if;
        raise notice 'PASS %: direct=%, function=%, summary=%, view=%', label, live_amount, function_amount, summary_amount, view_amount;
end $$;
select pg_temp.check_revenue('baseline without summary backfill', 36, 0, 36);

-- Capture the baseline result before changing source data.
select
    'baseline' as case_name,
    r.operator_id,
    p.created_utc::date as revenue_date,
    sum(p.amount) as captured_amount,
    count(*) as captured_payments
from payments p
join tickets t on t.id = p.ticket_id
join trips tr on tr.id = t.trip_id
join routes r on r.id = tr.route_id
where p.status = 'Captured'
group by r.operator_id, p.created_utc::date
order by r.operator_id, p.created_utc::date;

-- 1. Captured payment insert. Compare all four approaches.
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-CAPTURED', 'USER-1', 'TICKET-1', 'gateway-case-captured',
    36, 'DKK', 'Captured', '2026-04-29 10:00:00+00'
);
select pg_temp.check_revenue('captured insert', 72, 36, 36);

-- 2. Failed payment insert. It should not contribute to captured revenue.
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-FAILED', 'USER-1', 'TICKET-1', 'gateway-case-failed',
    50, 'DKK', 'Failed', '2026-04-29 10:05:00+00'
);
select pg_temp.check_revenue('failed insert', 72, 36, 36);

-- 3. Status correction from Failed to Captured.
update payments
set status = 'Captured'
where id = 'PAY-CASE-FAILED';
select pg_temp.check_revenue('status correction', 122, 36, 36);

-- 4. Status correction from Captured to Refunded.
update payments
set status = 'Refunded'
where id = 'PAY-CASE-CAPTURED';
select pg_temp.check_revenue('refund correction', 86, 36, 36);

-- 5. Delete or replace test data.
delete from payments
where id = 'PAY-CASE-FAILED';
select pg_temp.check_revenue('delete correction', 36, 36, 36);

-- 6. Duplicate delivery of an external payment reference.
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAY-CASE-DUPLICATE', 'USER-1', 'TICKET-1', 'gateway-capture-0001',
    36, 'DKK', 'Captured', '2026-04-29 10:10:00+00'
);
select pg_temp.check_revenue('duplicate accepted by permissive baseline', 72, 72, 36);

-- Refresh only after observing staleness.
refresh materialized view daily_captured_revenue;
select pg_temp.check_revenue('explicit refresh', 72, 72, 72);
rollback;
\echo 8 reporting comparison checks passed; experiment writes rolled back.
