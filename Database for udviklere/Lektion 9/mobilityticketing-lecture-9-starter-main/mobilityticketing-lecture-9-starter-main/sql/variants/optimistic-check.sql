
CREATE OR REPLACE FUNCTION lab09.book(
    p_request_id text,
    p_passenger_id text,
    p_trip_id text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_observed integer;
    v_booking_id bigint;
    v_updated_trip_id text;
BEGIN
    -- Read the current reserved-seat count.
    -- This SELECT does not explicitly lock the trip.
    SELECT reserved_seats
    INTO v_observed
    FROM lab09.trips
    WHERE id = p_trip_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Trip does not exist: %', p_trip_id;
    END IF;

    -- Update only if the counter still matches our earlier read.
    UPDATE lab09.trips
    SET reserved_seats = reserved_seats + 1
    WHERE id = p_trip_id
      AND status = 'Scheduled'
      AND reserved_seats = v_observed
      AND reserved_seats < capacity
    RETURNING id INTO v_updated_trip_id;

    -- No matching row means the optimistic check failed.
    IF NOT FOUND THEN
        RAISE EXCEPTION
            'Optimistic check failed for trip: %', p_trip_id;
    END IF;

    -- Insert the booking in the same transaction.
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
