select
    r.operator_id,
    p.created_utc::date as revenue_date,
    public.captured_revenue_for_day(r.operator_id,p.created_utc::date) as revenue_value
from payments p
join tickets t on t.id = p.ticket_id
join trips tr on tr.id = t.trip_id
join routes r on r.id = tr.route_id