set timezone = 'UTC';

select 'direct query' as approach,
       coalesce(sum(p.amount), 0) as captured_amount,
       count(*) as captured_payments
from payments p
join tickets t on t.id = p.ticket_id
join trips tr on tr.id = t.trip_id
join routes r on r.id = tr.route_id
where r.operator_id = 'OP-METRO'
  and p.created_utc::date = date '2026-04-29'
  and p.status = 'Captured'

union all

select 'function', captured_amount, captured_payments
from captured_revenue_for_day('OP-METRO', date '2026-04-29')

union all

select 'materialized view', captured_amount, captured_payments
from daily_captured_revenue
where operator_id = 'OP-METRO'
  and revenue_date = date '2026-04-29'

union all

select 'trigger summary', captured_amount, captured_payments
from daily_revenue_by_operator
where operator_id = 'OP-METRO'
  and revenue_date = date '2026-04-29';