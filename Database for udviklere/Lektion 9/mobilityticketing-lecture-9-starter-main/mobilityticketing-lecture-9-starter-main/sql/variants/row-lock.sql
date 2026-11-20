
CREATE OR REPLACE FUNCTION lab09.book(
    p_request_id text,
    p_passenger_id text,
    p_trip_id text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_status text;
    v_capacity integer;
    v_reserved integer;
    v_booking_id bigint;
BEGIN
    -- Lock the individual trip before checking availability.
    SELECT status, capacity, reserved_seats
    INTO v_status, v_capacity, v_reserved
    FROM lab09.trips
    WHERE id = p_trip_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Trip does not exist: %', p_trip_id;
    END IF;

    -- Check availability while holding the row lock.
    IF v_status <> 'Scheduled'
       OR v_reserved >= v_capacity THEN
        RAISE EXCEPTION 'Trip is unavailable: %', p_trip_id;
    END IF;

    -- Reserve one seat.
    UPDATE lab09.trips
    SET reserved_seats = reserved_seats + 1
    WHERE id = p_trip_id;

    -- Insert the booking.
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
