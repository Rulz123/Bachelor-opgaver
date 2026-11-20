
CREATE OR REPLACE FUNCTION lab09.book(
    p_request_id text,
    p_passenger_id text,
    p_trip_id text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_booking_id bigint;
    v_existing_id bigint;
    v_existing_passenger text;
    v_existing_trip text;
    v_updated_trip_id text;
BEGIN
    -- 1. Check whether this request has already succeeded.
    SELECT id, passenger_id, trip_id
    INTO v_existing_id, v_existing_passenger, v_existing_trip
    FROM lab09.bookings
    WHERE request_id = p_request_id;

    IF FOUND THEN
        IF v_existing_passenger <> p_passenger_id
           OR v_existing_trip <> p_trip_id THEN
            RAISE EXCEPTION
                'Request ID % already belongs to another booking',
                p_request_id;
        END IF;

        RETURN v_existing_id;
    END IF;

    -- 2. Reserve a seat and insert the booking atomically.
    BEGIN
        UPDATE lab09.trips
        SET reserved_seats = reserved_seats + 1
        WHERE id = p_trip_id
          AND status = 'Scheduled'
          AND reserved_seats < capacity
        RETURNING id INTO v_updated_trip_id;

        -- 3. If no seat was reserved, check whether a
        -- concurrent identical request has just succeeded.
        IF NOT FOUND THEN
            SELECT id, passenger_id, trip_id
            INTO v_existing_id, v_existing_passenger, v_existing_trip
            FROM lab09.bookings
            WHERE request_id = p_request_id;

            IF FOUND THEN
                IF v_existing_passenger <> p_passenger_id
                   OR v_existing_trip <> p_trip_id THEN
                    RAISE EXCEPTION
                        'Request ID % already belongs to another booking',
                        p_request_id;
                END IF;

                RETURN v_existing_id;
            END IF;

            RAISE EXCEPTION
                'Trip does not exist, is cancelled or is full: %',
                p_trip_id;
        END IF;

        -- 4. Insert the booking.
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

    EXCEPTION WHEN unique_violation THEN
        -- 5. A concurrent transaction may have inserted
        -- the same request ID. The exception block rolls
        -- back our attempted counter update.
        SELECT id, passenger_id, trip_id
        INTO v_existing_id, v_existing_passenger, v_existing_trip
        FROM lab09.bookings
        WHERE request_id = p_request_id;

        IF NOT FOUND THEN
            RAISE;
        END IF;

        IF v_existing_passenger <> p_passenger_id
           OR v_existing_trip <> p_trip_id THEN
            RAISE EXCEPTION
                'Request ID % already belongs to another booking',
                p_request_id;
        END IF;

        RETURN v_existing_id;
    END;

    RETURN v_booking_id;
END;
$$;
