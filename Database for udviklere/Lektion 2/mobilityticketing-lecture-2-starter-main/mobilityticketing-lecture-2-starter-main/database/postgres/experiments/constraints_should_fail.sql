-- Run each statement separately after applying your integrity migration.
-- Every statement below should be rejected by a named constraint.

-- 1. Negative capacity. Expected: CHECK violation.
update trips
set capacity = -1
where id = 'TRIP-M2-20260429-0800';

ERROR:  new row for relation "trips" violates check constraint "trips_check"
Failing row contains (TRIP-M2-20260429-0800, LINE-M2, 2026-04-29, 2026-04-29 08:00:00+00, Scheduled, -1, 2). 

SQL state: 23514
Detail: Failing row contains (TRIP-M2-20260429-0800, LINE-M2, 2026-04-29, 2026-04-29 08:00:00+00, Scheduled, -1, 2).

-- 2. More reserved seats than capacity. Expected: CHECK violation.
update trips
set reserved_seats = capacity + 1
where id = 'TRIP-M2-20260429-0800';

ERROR:  new row for relation "trips" violates check constraint "trips_check"
Failing row contains (TRIP-M2-20260429-0800, LINE-M2, 2026-04-29, 2026-04-29 08:00:00+00, Scheduled, 120, 121). 

SQL state: 23514
Detail: Failing row contains (TRIP-M2-20260429-0800, LINE-M2, 2026-04-29, 2026-04-29 08:00:00+00, Scheduled, 120, 121).

-- 3. Unknown trip. Expected: FOREIGN KEY violation.
insert into tickets (
    id, user_id, trip_id, ticket_code, status,
    product_code, valid_from_utc, valid_to_utc, price, currency
) values (
    'T-INVALID-TRIP', 'USER-1', 'TRIP-DOES-NOT-EXIST',
    'CODE-INVALID-TRIP', 'Active', 'SINGLE',
    '2026-04-29 08:00:00+00', '2026-04-29 09:00:00+00', 36, 'DKK'
);

ERROR:  insert or update on table "tickets" violates foreign key constraint "fk_trip_id"
Key (trip_id)=(TRIP-DOES-NOT-EXIST) is not present in table "trips". 

SQL state: 23503
Detail: Key (trip_id)=(TRIP-DOES-NOT-EXIST) is not present in table "trips".

-- 4. Reversed validity window. Expected: CHECK violation.
insert into tickets (
    id, user_id, trip_id, ticket_code, status,
    product_code, valid_from_utc, valid_to_utc, price, currency
) values (
    'T-REVERSED', 'USER-1', 'TRIP-M2-20260429-0800',
    'CODE-REVERSED', 'Active', 'SINGLE',
    '2026-04-29 09:00:00+00', '2026-04-29 08:00:00+00', 36, 'DKK'
);

ERROR:  new row for relation "tickets" violates check constraint "ticket_time_validation"
Failing row contains (T-REVERSED, USER-1, TRIP-M2-20260429-0800, CODE-REVERSED, Active, SINGLE, 2026-04-29 09:00:00+00, 2026-04-29 08:00:00+00, 36, DKK). 

SQL state: 23514
Detail: Failing row contains (T-REVERSED, USER-1, TRIP-M2-20260429-0800, CODE-REVERSED, Active, SINGLE, 2026-04-29 09:00:00+00, 2026-04-29 08:00:00+00, 36, DKK).

-- 5. Duplicate ticket code. Expected: UNIQUE violation.
insert into tickets (
    id, user_id, trip_id, ticket_code, status,
    product_code, valid_from_utc, valid_to_utc, price, currency
)
select
    'T-DUPLICATE-CODE', user_id, trip_id, ticket_code, status,
    product_code, valid_from_utc, valid_to_utc, price, currency
from tickets
where id = 'TICKET-1';

ERROR:  duplicate key value violates unique constraint "tickets_ticket_code_key"
Key (ticket_code)=(CODE-M2-0001) already exists. 

SQL state: 23505
Detail: Key (ticket_code)=(CODE-M2-0001) already exists.

-- 6. Unknown ticket status. Expected: CHECK violation.
update tickets
set status = 'Unknown'
where id = 'TICKET-1';

ERROR:  new row for relation "tickets" violates check constraint "tickets_status_check"
Failing row contains (TICKET-1, USER-1, TRIP-M2-20260429-0800, CODE-M2-0001, Unknown, SINGLE, 2026-04-29 07:45:00+00, 2026-04-29 10:00:00+00, 36.00, DKK). 

SQL state: 23514
Detail: Failing row contains (TICKET-1, USER-1, TRIP-M2-20260429-0800, CODE-M2-0001, Unknown, SINGLE, 2026-04-29 07:45:00+00, 2026-04-29 10:00:00+00, 36.00, DKK).

-- 7. Negative product price. Expected: CHECK violation.
update products
set price = -1
where code = 'SINGLE';

ERROR:  new row for relation "products" violates check constraint "products_price_check"
Failing row contains (SINGLE, Single trip, -1, DKK). 

SQL state: 23514
Detail: Failing row contains (SINGLE, Single trip, -1, DKK).

-- 8. Payment for an unknown ticket. Expected: FOREIGN KEY violation.
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status
) values (
    'PAYMENT-UNKNOWN-TICKET', 'USER-1', 'NO-SUCH-TICKET',
    'gateway-capture-invalid', 36, 'DKK', 'Captured'
);

ERROR:  insert or update on table "payments" violates foreign key constraint "fk_ticket_id"
Key (ticket_id)=(NO-SUCH-TICKET) is not present in table "tickets". 

SQL state: 23503
Detail: Key (ticket_id)=(NO-SUCH-TICKET) is not present in table "tickets".

-- 9. Duplicate external payment reference. Expected: UNIQUE violation.
insert into payments (
    id, user_id, ticket_id, external_payment_reference,
    amount, currency, status
) values (
    'PAYMENT-DUPLICATE-REFERENCE', 'USER-1', 'TICKET-1',
    'gateway-capture-0001', 36, 'DKK', 'Captured'
);

ERROR:  duplicate key value violates unique constraint "payments_external_payment_reference_key"
Key (external_payment_reference)=(gateway-capture-0001) already exists. 

SQL state: 23505
Detail: Key (external_payment_reference)=(gateway-capture-0001) already exists.

-- 10. Mismatched ticket id and ticket code. Expected: composite FOREIGN KEY violation.
insert into validations (
    id, ticket_id, ticket_code, vehicle_id, stop_id, device_id, result
) values (
    'VALIDATION-MISMATCH', 'TICKET-1', 'CODE-5C-0001',
    'BUS-5C-01', 'STOP-CENTRAL', 'DEVICE-01', 'Accepted'
);

ERROR:  duplicate key value violates unique constraint "validations_pkey"
Key (id)=(VALIDATION-MISMATCH) already exists. 

SQL state: 23505
Detail: Key (id)=(VALIDATION-MISMATCH) already exists.