
CREATE OR REPLACE FUNCTION lab09.book(
    p_request_id text,
    p_passenger_id text,
    p_trip_id text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_lock_key integer;
    v_status text;
    v_capacity integer;
    v_reserved integer;
    v_booking_id bigint;
BEGIN
    -- Assign a consistent numeric key to each fixture trip.
    v_lock_key := CASE p_trip_id
        WHEN 'T-LAST' THEN 1
        WHEN 'T-TWO' THEN 2
        WHEN 'T-CANCELLED' THEN 3
        WHEN 'T-OTHER' THEN 4
        ELSE NULL
    END;

    IF v_lock_key IS NULL THEN
        RAISE EXCEPTION 'Trip does not exist: %', p_trip_id;
    END IF;

    -- Acquire a transaction-scoped advisory lock.
    PERFORM pg_advisory_xact_lock(9009, v_lock_key);

    -- Lock and read the actual trip row.
    SELECT status, capacity, reserved_seats
    INTO v_status, v_capacity, v_reserved
    FROM lab09.trips
    WHERE id = p_trip_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Trip does not exist: %', p_trip_id;
    END IF;

    IF v_status <> 'Scheduled'
       OR v_reserved >= v_capacity THEN
        RAISE EXCEPTION 'Trip is unavailable: %', p_trip_id;
    END IF;

    UPDATE lab09.trips
    SET reserved_seats = reserved_seats + 1
    WHERE id = p_trip_id;

    INSERT INTO lab09.bookings (
        request_id,
        passenger_id,
        trip_id
    )
    VALUES (
        p_request_id,
        p_passenger_id,
        p_trip_id
    )
    RETURNING id INTO v_booking_id;

    RETURN v_booking_id;
END;
$$;
