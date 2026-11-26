-- Write your experimental DROP INDEX and CREATE INDEX statements here.
-- Test one candidate at a time. Drop only indexes you created for this experiment.
-- The primary-key index is part of the starting schema and must stay.

--Candidate A:
--DROP INDEX IF EXISTS lab08.index_trips_route_departure_status;
--DROP INDEX IF EXISTS lab08.index_trips_route;

--CREATE INDEX index_trips_route
--ON lab08.trips (route_id);

--Candidate B:

DROP INDEX IF EXISTS lab08.index_trips_route;

DROP INDEX IF EXISTS lab08.index_trips_route_departure_status;

CREATE INDEX index_trips_route_departure_status
ON lab08.trips
(route_id, scheduled_departure_utc, id)
WHERE status = 'Scheduled';

--Route only:
--DROP INDEX IF EXISTS lab08.index_trips_route_departure_status;
--DROP INDEX IF EXISTS lab08.index_trips_route;

--CREATE INDEX index_trips_route
--ON lab08.trips (route_id);