-- Your query goes here. It starts as a copy of the baseline.
-- Keep the columns, ordering and LIMIT. Rewrite the date predicate.
-- query_day is a UTC calendar date. result_limit is 20, or NULL for the full-day check.
select id, route_id, scheduled_departure_utc, status
from lab08.trips
where route_id = :'route_id'
  and status = 'Scheduled'
  and scheduled_departure_utc >= TIMESTAMP '2026-10-23 00:00:00'
  and scheduled_departure_utc <  TIMESTAMP '2026-10-24 00:00:00'
order by scheduled_departure_utc, id
limit :result_limit;
