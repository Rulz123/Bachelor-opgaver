\set ON_ERROR_STOP 1

begin;

\echo 'Lecture 3: baseline across all four strategies'
select (p.created_utc at time zone 'UTC')::date as revenue_date,
             p.currency,
             sum(p.amount)::numeric as captured_amount
from payments p
where (p.created_utc at time zone 'UTC')::date = '2026-04-29'
    and p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency;
select * from captured_revenue_for_day('2026-04-29', 'DKK');
select revenue_date, currency, captured_amount
from captured_revenue_daily
where revenue_date = '2026-04-29';
select revenue_date, currency, captured_amount
from captured_revenue_daily_mv
where revenue_date = '2026-04-29';

insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status, created_utc
) values (
    'PAYMENT-LECTURE3', 'USER-1', 'TICKET-1', 'lecture3-captured',
    10.00, 'DKK', 'Captured', '2026-04-29 10:00:00+00'
);

\echo 'After captured insert: trigger summary changes immediately; MV is stale'
select * from captured_revenue_for_day('2026-04-29', 'DKK');
select * from captured_revenue_daily where revenue_date = '2026-04-29';
select * from captured_revenue_daily_mv where revenue_date = '2026-04-29';

\echo 'Refresh while the temporary captured payment still exists: MV becomes 82.00'
refresh materialized view captured_revenue_daily_mv;
select * from captured_revenue_daily_mv where revenue_date = '2026-04-29';

update payments set status = 'Failed' where id = 'PAYMENT-LECTURE3';
update payments set status = 'Captured' where id = 'PAYMENT-LECTURE3';
update payments set status = 'Refunded' where id = 'PAYMENT-LECTURE3';

\echo 'After failed/captured/refunded transitions: direct and function agree with trigger summary'
select (p.created_utc at time zone 'UTC')::date as revenue_date,
             p.currency,
             sum(p.amount)::numeric as captured_amount
from payments p
where (p.created_utc at time zone 'UTC')::date = '2026-04-29'
    and p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency;
select * from captured_revenue_for_day('2026-04-29', 'DKK');
select * from captured_revenue_daily where revenue_date = '2026-04-29';

delete from payments where id = 'PAYMENT-LECTURE3';

\echo 'Cleanup: trigger summary returns to baseline; refresh restores MV to 72.00'
select (p.created_utc at time zone 'UTC')::date as revenue_date,
             p.currency,
             sum(p.amount)::numeric as captured_amount
from payments p
where (p.created_utc at time zone 'UTC')::date = '2026-04-29'
    and p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency;
select * from captured_revenue_for_day('2026-04-29', 'DKK');
select * from captured_revenue_daily where revenue_date = '2026-04-29';
refresh materialized view captured_revenue_daily_mv;
select * from captured_revenue_daily_mv where revenue_date = '2026-04-29';

do $$
begin
    begin
        insert into payments (
            id, user_id, ticket_id, external_payment_reference,
            amount, currency, status
        ) values (
            'PAYMENT-LECTURE3-DUP', 'USER-1', 'TICKET-1',
            'gateway-capture-0001', 1.00, 'DKK', 'Captured'
        );
        raise exception 'duplicate external reference was accepted';
    exception when unique_violation then
        raise notice 'expected failure: duplicate external payment reference rejected';
    end;
end;
$$;

rollback;