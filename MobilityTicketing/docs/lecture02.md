# Lecture 2: integrity catalogue

The database owns row-local invariants. Transaction/application services own
rules that require coordination across concurrent writes or external systems.
The negative checks in `database/postgres/experiments/lecture02_constraints.sql`
run in a transaction and report expected PostgreSQL constraint failures.

The historical `constraints_should_fail.sql` belongs to the pre-contract model
and tests `tickets.product_code`. Run it after migrations `011`-`018` but
before `030`-`032`. The maintained `lecture02_constraints.sql` belongs to the
final post-contract model and tests `tickets.product_id`; run it only after
`032_contract_product_identity.sql`. Expected named constraint failures are
success evidence. Unexpected missing-column, missing-constraint, or syntax
errors are verification failures.

| Rule | Class | Owner and mechanism | Name/evidence | State |
|---|---|---|---|---|
| Trip capacity is non-negative | Simple database constraint | PostgreSQL check | `trips_capacity_non_negative` | Implemented |
| Reserved seats are non-negative and do not exceed capacity | Simple database constraint | PostgreSQL check | `trips_reserved_seats_valid` | Implemented |
| Ticket price is finite and non-negative | Simple database constraint | PostgreSQL check | `tickets_price_valid` | Implemented |
| Payment amount is finite and non-negative | Simple database constraint | PostgreSQL check | `payments_amount_valid` | Implemented |
| Currency is a supported value | Simple database constraint | PostgreSQL check | `products_currency_supported`, `tickets_currency_supported`, `payments_currency_supported` | Implemented for seeded DKK model |
| Ticket code is unique | Unique constraint | PostgreSQL unique constraint | `tickets_code_unique` | Implemented |
| Payment references an existing ticket and user | Simple database constraint | PostgreSQL foreign keys | `payments_ticket_fk`, `payments_user_fk` | Implemented |
| Validation ticket identity matches one ticket | Simple database constraint | PostgreSQL composite foreign key | `validations_ticket_identity_fk` | Implemented |
| Ticket validity end is not before start | Simple database constraint | PostgreSQL check | `tickets_validity_order` | Implemented |
| Status values are from the current domain vocabulary | Simple database constraint | PostgreSQL checks | `trips_status_valid`, `tickets_status_valid`, `payments_status_valid`, `validations_result_valid` | Implemented |
| External payment reference is unique | Unique constraint | PostgreSQL unique constraint | `payments_external_reference_unique` | Implemented |
| Two concurrent reservations cannot oversell a trip | Multi-row or transaction-dependent rule | Reservation transaction must lock the trip row or use serializable coordination | Transaction/application service | Deferred |
| A payment capture matches the external provider | External-system rule | Payment integration reconciliation and idempotency | External integration | Deferred |

The status vocabulary preserves the values already present in the seed data:
`scheduled` and `Scheduled` for trips, `Active` and `Validated` for tickets,
`Captured`, `Failed`, and `Refunded` for payments, and `Accepted` or `Rejected`
for validations. Currency is currently deliberately limited to the seeded
`DKK` domain; expanding it requires a documented migration and reporting
policy for multi-currency totals.