# Integrity map

| Invariant | Affected tables and columns | Current protection | Missing protection or limitation | Expected failure behaviour | Evidence |
| --- | --- | --- | --- | --- | --- |
| Nonnegative capacity | trips.capacity | trips_capacity_nonnegative | Does not count bookings | 23514 | verify.sql |
| Valid reserved count | trips.reserved_seats | trips_reserved_valid | Requires transactional booking | 23514 | verify.sql |
| Nonnegative prices | products/tickets.price | products_price_valid, tickets_price_valid | Historical price policy separate | 23514 | verify.sql |
| Nonnegative payment | payments.amount | payments_amount_valid | Does not confirm gateway settlement | 23514 | migration |
| Currency representation | products/tickets/payments.currency | NOT NULL and uppercase 3-letter CHECKs | Does not validate an ISO currency catalogue | 23514/23502 | verify.sql |
| Ticket lookup identity | tickets.ticket_code | UNIQUE and NOT NULL | Codes are not authorization | 23505/23502 | verify.sql |
| Valid ticket user/trip/product | tickets references | Named FKs, NOT NULL | Disabled-user policy separate | 23503 | verify.sql |
| Valid time interval | tickets.valid_from/to | tickets_validity_valid | Scan-time validity separate | 23514 | verify.sql |
| Known statuses | tickets/payments.status | Named CHECKs | State transitions separate | 23514 | verify.sql |
| Existing payment ticket/user | payments references | Named FKs | User/ticket ownership still a workflow decision | 23503 | verify.sql |
| Unique captured reference | payments.external_payment_reference | Partial unique index for Captured | Gateway retry policy separate | 23505 | verify.sql |
| Matching ticket ID/code | validations pair | validations_ticket_identity_fk | Vehicle and device identity separate | 23503 | verify.sql |

The migration is `database/postgres/migrations/011_ticketing_integrity.sql`.
The current executable evidence is `database/postgres/experiments/verify.sql`,
12 passing checks with rollback; see [actual logs and commands](../../../../validation/README.md).
The old pasted negative-write output is not the automated test suite.

## Issue register

### Issue 1

- Evidence: trips_reserved_valid only reads one trip row; it does not count tickets.
- Problem: separate read/insert/update operations can disagree under concurrency.
- Consequence: a valid counter alone does not prove that bookings fit capacity.
- Specific improvement: one transaction for the capacity decision and booking write, tested in later lessons.
- Open question: which cancellations and ticket types release reserved capacity?

### Issue 2

- Evidence: payment FKs and the captured-reference index validate stored rows only.
- Problem: external gateway capture is not atomic with PostgreSQL commit.
- Consequence: a timeout can leave a captured payment without confirmed local state.
- Specific improvement: stable payment identity, outcome verification and explicit recovery; do not blindly recharge.
- Open question: retention and refund policy for historical payment and validation records.

## State-transition trace

### Ticket purchase

1. Resolve an existing user, trip and product; apply business policies outside these row constraints.
2. Insert a ticket with a unique code, agreed historical price, currency and valid interval.
3. Record a payment referencing the ticket/user. Coordinate gateway capture separately; a FK is not payment confirmation.

### Ticket validation

1. Resolve the ticket by its unique code and evaluate current validity and status.
2. Insert a validation carrying the matching ticket ID/code pair and stop reference.
3. Retain historical rows; default FK restriction prevents deleting referenced tickets. Retention and status transitions require policy.
