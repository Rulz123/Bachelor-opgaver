-- Direct strategy. Captured revenue is the UTC calendar-day sum of payments
-- whose status is exactly 'Captured', grouped by currency.
select (p.created_utc at time zone 'UTC')::date as revenue_date,
       p.currency,
       sum(p.amount)::numeric as captured_amount
from payments p
where (p.created_utc at time zone 'UTC')::date = :revenue_date
  and p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency
order by p.currency;

-- Function strategy.
select * from captured_revenue_for_day(:revenue_date, null);

-- Trigger-maintained summary strategy.
select revenue_date, currency, captured_amount
from captured_revenue_daily
where revenue_date = :revenue_date
order by currency;

-- Materialized-view strategy.
select revenue_date, currency, captured_amount
from captured_revenue_daily_mv
where revenue_date = :revenue_date
order by currency;