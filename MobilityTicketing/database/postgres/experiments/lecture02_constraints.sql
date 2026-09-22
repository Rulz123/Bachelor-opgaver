\set ON_ERROR_STOP 1

begin;

\echo 'Valid write: reserved seats within trip capacity'
update trips
set reserved_seats = reserved_seats + 1
where id = 'TRIP-M2-20260429-0800'
    and reserved_seats < capacity;
\echo 'Valid write succeeded under trips_reserved_seats_valid'

\echo 'Valid write: non-negative captured payment amount'
insert into payments (
        id, user_id, ticket_id, external_payment_reference,
        amount, currency, status
) values (
        'VALID-PAYMENT-LECTURE2', 'USER-1', 'TICKET-1',
        'lecture2-valid', 1.00, 'DKK', 'Captured'
);
\echo 'Valid write succeeded under payments_amount_valid'

do $$
begin
    begin
        update trips set reserved_seats = capacity + 1 where id = 'TRIP-M2-20260429-0800';
        raise exception 'reserved seat overflow was accepted';
    exception when check_violation then
        raise notice 'expected failure: trips_reserved_seats_valid';
    end;
end;
$$;

do $$
begin
    begin
        insert into tickets (
            id, user_id, trip_id, ticket_code, status, product_id,
            valid_from_utc, valid_to_utc, price, currency
        ) values (
            'INVALID-TICKET', 'USER-1', 'TRIP-M2-20260429-0800',
            'CODE-M2-INVALID', 'Active', 1,
            '2026-04-29 10:00:00+00', '2026-04-29 09:00:00+00',
            1.00, 'DKK'
        );
        raise exception 'invalid ticket was accepted';
    exception when check_violation then
        raise notice 'expected failure: tickets_validity_order';
    end;
end;
$$;

do $$
begin
    begin
        insert into tickets (
            id, user_id, trip_id, ticket_code, status, product_id,
            valid_from_utc, valid_to_utc, price, currency
        ) values (
            'INVALID-DUPLICATE', 'USER-1', 'TRIP-M2-20260429-0800',
            'CODE-M2-0001', 'Active', 1,
            '2026-04-29 09:00:00+00', '2026-04-29 10:00:00+00',
            1.00, 'DKK'
        );
        raise exception 'duplicate ticket code was accepted';
    exception when unique_violation then
        raise notice 'expected failure: tickets_code_unique';
    end;
end;
$$;

do $$
begin
    begin
        insert into payments (
            id, user_id, ticket_id, external_payment_reference,
            amount, currency, status
        ) values (
            'INVALID-PAYMENT', 'USER-1', 'TICKET-1',
            'lecture2-invalid', -1.00, 'DKK', 'Captured'
        );
        raise exception 'negative payment was accepted';
    exception when check_violation then
        raise notice 'expected failure: payments_amount_valid';
    end;
end;
$$;

do $$
begin
    begin
        update payments set status = 'Unknown' where id = 'PAYMENT-1';
        raise exception 'unknown payment status was accepted';
    exception when check_violation then
        raise notice 'expected failure: payments_status_valid';
    end;
end;
$$;

rollback;