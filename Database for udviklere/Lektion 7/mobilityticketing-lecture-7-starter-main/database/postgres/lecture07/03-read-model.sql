-- Design persistent storage for published dispatch plans in lab07.
-- Include the plan identity, time window, publication time and displayed values.
-- The contract defines behavior; the table layout is your choice.


CREATE TABLE lab07.dispatch_plans (
    id TEXT PRIMARY KEY,
    route_id TEXT NOT NULL,
    departure TIMESTAMPTZ NOT NULL,
    arrival TIMESTAMPTZ NOT NULL,
    published_at TIMESTAMPTZ NOT NULL,
    status TEXT NOT NULL CHECK (
        status IN (
            'Scheduled',
            'Cancelled'
        )
    ),
    number_of_stops INTEGER NOT NULL
);


CREATE OR REPLACE FUNCTION lab07.calculate_number_of_stops()
RETURNS TRIGGER AS $$
BEGIN
    SELECT COUNT(*)
    INTO NEW.number_of_stops
    FROM lab07.trips
    WHERE trips.route_id = NEW.route_id;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


CREATE TRIGGER set_number_of_stops
BEFORE INSERT OR UPDATE OF route_id
ON lab07.dispatch_plans
FOR EACH ROW
EXECUTE FUNCTION lab07.calculate_number_of_stops();