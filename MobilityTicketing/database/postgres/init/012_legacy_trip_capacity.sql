-- Test-data assumption: use the same capacities as the lecture 2 examples.
-- Only fill missing capacities for the four lecture 1 trips.

update trips
set capacity = 80
where id in ('TRIP-5C-001', 'TRIP-5C-002')
  and capacity is null;

update trips
set capacity = 120
where id in ('TRIP-M2-001', 'TRIP-M2-002')
  and capacity is null;
