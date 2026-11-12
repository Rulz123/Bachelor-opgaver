-- Write your experimental DROP INDEX and CREATE INDEX statements here.
-- Test one candidate at a time. Drop only indexes you created for this experiment.
-- The primary-key index is part of the starting schema and must stay.
--create index index_trips_scheduled_departure on lab08.trips (route_id) if not exists;

create index index_trips_scheduled_departure on lab08.trips (scheduled_departure_utc) if not exists;

drop index if exists index_trips_scheduled_departure;