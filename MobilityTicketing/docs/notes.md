# MobilityTicketing — Learning notes

Notes, design decisions and evidence from the MobilityTicketing labs. Each lecture records the work at that stage; later lectures may address earlier limitations.

## Contents

- [Lecture 1 — Relational modelling](#lecture-1--relational-modelling)
- [Lecture 2 — SQL operations and constraints](#lecture-2--sql-operations-and-constraints)
- [Lecture 3 — SQL programmability](#lecture-3--sql-programmability)
- [Lecture 4 — Product-identity migration](#lecture-4--product-identity-migration)

## Lecture 1 — Relational modelling

### Task

Build a small relational model for operators, routes, stops, route stops and trips. Add seed data, document the relationships, and implement the three route/timetable queries.

### System context

Customers use MobilityTicketing to find routes and stops,
view departures and delays, buy tickets, and validate them
when boarding.

Transport operators use it to maintain routes and timetables,
update ticket products and prices, and review usage and revenue.

### Workload map

| Workload | Main data | Access | Main concern |
|---|---|---|---|
| Journey search | Routes, stops, departures, prices, availability | Mostly reads | Fast results; slight staleness acceptable |
| Ticket purchase | Capacity, products, tickets, payments | Reads and writes | Correct payment and reservation; prevent overselling |
| Ticket validation | Tickets, product rules, validations | Reads and writes | Correct acceptance and fast boarding |
| Timetable maintenance | Routes and trips | Reads and writes | Correct updates that become visible to searches |
| Real-time availability | Capacity and reservations | Frequent reads; updates as reservations change | Distinguish browsing availability from purchase guarantees |
| Reporting | Tickets, payments, validations | Mostly reads | Correct totals; more delay is acceptable |

### Design decision: route-stop identity

A route may visit the same stop more than once.
The primary key is (route_id, stop_sequence), so each
position is unique within its route.

### What I learned: functional dependencies and normalization

In operators, id determines name: id → name.
Each operator ID identifies exactly one operator name,
but different operators may share a name.

We store the operator name in operators, rather than
copying it into routes. Routes reference operators.id.

This prevents an update anomaly: renaming an operator
requires changing only one row, rather than updating
a separate copy of the name on every route.

### Evidence: seed repeatability

Reran 002_seed.sql after the initial load.
All five INSERT statements reported INSERT 0 0.
Existing seed rows were skipped without errors.

### Evidence: schema and queries

- The schema and seed data loaded into an empty database.
- Rerunning the seed inserted no duplicate rows.
- The upcoming-trips query returned the expected scheduled trip.
- The ordered-stops query returned stops in route sequence.
- The trip-count query returned two trips per route on the
  seeded date and preserved both routes with zero counts
  on a date without trips.

Evidence files: [ER diagram](lecture01/MobilityTicketing-ERD.md), [query results](lecture01/lecture1-query-results.txt), and [SQL queries](../database/postgres/003_queries.sql).

### Limitations at the end of Lecture 1

- Purchases, payments and ticket validation are not implemented.
- Capacity management and concurrent purchases are not implemented.
- Trip status is required, but its allowed values are not restricted.
- Stop positions must be positive and unique within a route,
  but they do not have to be consecutive.
- The schema does not enforce that a route and its stops
  belong to the same city.
- We have not yet tested deliberate constraint violations
  or performance with large datasets.

### Open question: temporary reservations

Should starting checkout temporarily reserve a seat?
If so, we need rules for expiration and abandoned checkouts.
Viewing a trip currently does not guarantee a seat.

## Lecture 2 — SQL operations and constraints

### Task

Strengthen the supplied ticketing schema through migrations. Classify the business rules, test successful and rejected writes, and explain the remaining workflow and historical-data limitations.

### Implementation

Migrations 011–016 strengthen the supplied ticketing schema.
The initial draft schema remains unchanged.
Missing capacities on Lecture 1 test trips were filled using
the starter's example capacities: M2 = 120, 5C = 80.

### Integrity map

The database owns the enforced rules below. Required values
also use NOT NULL. Migration numbers identify the SQL files.

| Rule | Tables | Enforcement | Migration | Limitation |
|---|---|---|---|---|
| Capacity is known and nonnegative | trips | NOT NULL + CHECK | 011 | Capacity values themselves are test assumptions |
| Reservations are between zero and capacity | trips | NOT NULL + CHECK | 011 | Does not implement a complete concurrent purchase workflow |
| Ticket price is known and nonnegative | tickets | NOT NULL + CHECK | 012 | Does not verify the correct purchase price |
| Ticket codes are present and unique | tickets | NOT NULL + UNIQUE | 012 | Does not prevent unauthorized use of a code |
| Tickets reference existing users | tickets, users | NOT NULL + FK | 012 | Does not reject disabled users |
| Tickets reference existing trips and products | tickets, trips, products | NOT NULL + FK | 013 | Does not check whether a trip is still sellable |
| Ticket validity ends at or after its start | tickets | NOT NULL + CHECK | 013 | Does not check validity at boarding time |
| Currency uses three uppercase letters | tickets, payments, products | NOT NULL + CHECK | 013, 014, 016 | Does not validate real currency codes or agreement between rows |
| Status belongs to the chosen allowed set | tickets, payments, trips | NOT NULL + CHECK | 013, 014, 016 | Does not enforce status transitions |
| Payment references existing user and ticket | payments | NOT NULL + FK | 014 | Payer may differ from ticket owner |
| Payment amount is known and nonnegative | payments | NOT NULL + CHECK | 014 | Does not prove external payment capture |
| External payment reference is present and unique | payments | NOT NULL + UNIQUE | 014 | Assumes globally unique references in our stored gateway data |
| Validation ticket ID and code identify the same ticket | validations, tickets | NOT NULL + composite FK | 015 | Does not prove the ticket may be accepted |
| Validation references an existing stop | validations, stops | NOT NULL + FK | 015 | Does not verify that the stop belongs to the ticket's route |
| Validation result is Accepted or Rejected | validations | NOT NULL + CHECK | 015 | Unknown-ticket scans need a separate design |
| Product price is known and nonnegative | products | NOT NULL + CHECK | 016 | Existing tickets retain their own purchase price |
| Product name and event timestamps are present | products, payments, validations | NOT NULL | 014–016 | Presence does not establish accuracy |

### What I learned: rule classification

- Direct column/table rules: required values, nonnegative amounts,
  capacity bounds, validity ordering, formats and allowed values.
- Uniqueness rules: ticket codes and external payment references.
- Referential rules: foreign keys, including validation ID/code matching.
- Cross-row or external workflow rules: reliable payment capture,
  coordinated purchase changes and checking eligibility at validation.
- Domain decisions: permitted status transitions, whether another
  person may pay, temporary reservations and retention periods.

### What I learned: database error codes

- 23502: NOT NULL violation.
- 23503: foreign-key violation.
- 23505: uniqueness violation.
- 23514: CHECK violation.

These SQLSTATE codes identify error categories without depending
on the language of PostgreSQL's error messages.

### Evidence: relationship tests

Deleting TICKET-1 failed because PAYMENT-1 references it.
Changing PAYMENT-1's user to USER-2 succeeded, even though
TICKET-1 belongs to USER-1.

Both tests were rolled back. This demonstrates that existing
references are enforced, but payer/owner equality is not.

### Evidence: automated tests

experiments/lecture02/lecture2-integrity-tests.sql completed with 99 passed tests.
Output is saved in docs/lecture02/lecture2-test-results.txt.
Tests check successful writes and expected SQLSTATE codes,
including the responsible constraint or column where applicable.
All test changes are rolled back.

Evidence files: [test script](../database/postgres/experiments/lecture02/lecture2-integrity-tests.sql), [test results](lecture02/lecture2-test-results.txt), and [migrations](../database/postgres/migrations/lecture02/).

### Workflow: ticket purchase

1. Read the user, trip and product; check purchase eligibility,
   availability and the current price.
2. Insert a ticket referencing the existing user, trip and product.
   Store the agreed price and currency on the ticket.
3. Record payment information referencing the existing ticket and user.
4. Update the trip's reservation count after a successful purchase.

These are the affected rows, not a complete payment protocol.
Database changes and the external gateway need coordinated handling
of failures and retries. This lab has not implemented that workflow.

### Workflow: ticket validation

1. Find the ticket by its unique ticket code.
2. Check its status, validity period and product rules.
3. Insert a validation referencing the matching ticket ID/code
   and an existing stop, with the result and timestamp.
4. If the product rules require it, update the ticket status.

The constraints enforce references and permitted values.
They do not perform the acceptance decision or prevent every
form of repeated use.

### Design decision: deletion and historical meaning

Our foreign keys use default NO ACTION behaviour for deletes
and referenced-key updates.

- Referenced users, trips and products cannot be deleted while
  tickets reference them.
- Referenced users and tickets cannot be deleted while payments
  reference them.
- Referenced ticket ID/code pairs and stops cannot be deleted
  while validations reference them.
- Updating referenced keys is rejected if it would break these links.

However, non-key changes may still change historical meaning.
For example, a trip's route or a payment's valid ticket reference
can be changed. Constraints alone do not provide an audit trail.

Proposed policy: retain historical tickets, payments and validations;
disable or retire operational records instead of deleting them.
Retention periods and permitted corrections still need domain decisions.
This policy is documented, not fully implemented.

Unreferenced rows can still be deleted. Referenced payments and validations are not automatically deleted. A complete retention or soft-deletion policy is not implemented.

### Limitations and open questions

- Reservation counts must stay between zero and capacity,
  but this does not implement a safe concurrent purchase workflow.
- Payment constraints do not prove that an external gateway
  actually captured money.
- Separate payment-user and ticket references do not require
  the payer to be the ticket owner. This needs a domain decision.
- Allowed status values do not enforce allowed status transitions.
- Currency checks enforce three uppercase letters, not membership
  in a real currency catalogue.

## Lecture 3 — SQL programmability

### Task

Compare four ways to calculate daily captured revenue:
a direct query, SQL function, materialized view and
trigger-maintained summary.

Test how they handle inserts, corrections, refunds,
deletions and duplicate payment delivery.

### What I learned

- The direct query calculates totals from the payment records
  whenever it runs.
- The SQL function packages the same calculation behind a
  reusable name and parameters. It does not store the result.
- A materialized view stores a snapshot. Changes to payments
  only appear after REFRESH MATERIALIZED VIEW.
- Our trigger updates a summary automatically after payment
  inserts. It ignores updates and deletes, so corrections
  can leave the summary incorrect.
- An apparently correct result can be coincidental: the stale
  materialized view matched again when the underlying total
  returned to its original value.

### Report definition and assumptions

Payments are the source of truth for this experiment.
We sum payments whose current status is Captured, grouped
by their creation date in UTC and the route's current operator.
All example amounts are DKK.

This is not a separate historical record of capture and refund
events. Changing a payment's status can change a past day's total.

### Evidence: observed results

OP-METRO, 2026-04-29. Amounts are DKK.

| After action | Direct | Function | Materialized view | Trigger |
|---|---:|---:|---:|---:|
| Initial setup | 36 | 36 | 36 | 36 |
| Captured insert | 72 | 72 | 36 | 72 |
| Failed insert | 72 | 72 | 36 | 72 |
| Failed changed to Captured | 122 | 122 | 36 | 72 |
| Captured changed to Refunded | 86 | 86 | 36 | 72 |
| Test payment deleted | 36 | 36 | 36 | 72 |
| Duplicate attempted | 36 | 36 | 36 | 72 |
| Materialized view refreshed | 36 | 36 | 36 | 72 |
| Trigger summary rebuilt | 36 | 36 | 36 | 36 |

### Evidence: duplicate delivery and recovery

The Lecture 2 unique payment-reference constraint rejected
the duplicate with SQLSTATE 23505. No totals changed.

Refreshing recalculates the materialized view.
Rebuilding replaces the trigger summary with totals calculated
from the base records. Repairing the total does not fix the
trigger's missing UPDATE and DELETE handling.

The rebuild was performed without concurrent payment writes.

Evidence files: [baseline](lecture03/lecture3-baseline.txt), [function](lecture03/lecture3-function-results.txt), [materialized view](lecture03/lecture3-materialized-view-results.txt), [trigger baseline](lecture03/lecture3-trigger-baseline.txt), [inserts](lecture03/lecture3-inserts-results.txt), [corrections](lecture03/lecture3-corrections-results.txt), and [recovery](lecture03/lecture3-recovery-results.txt).

### Design decision: current recommendation

I recommend the direct query for this implementation because
it reflected payment changes without a separate total to maintain.
The SQL function was equally correct and would provide a shared
definition if several callers needed the calculation.

We have not measured performance. Correct reporting also depends
on correct payment records.

### Responsibility matrix

Payments are the source of truth. Operator attribution follows
the current ticket → trip → route relationships.

| Aspect | Direct query | SQL function | Materialized view | Insert-only trigger |
|---|---|---|---|---|
| Correctness | Recalculates from visible base records | Same calculation | Correct as of last refresh | Misses updates and deletes |
| Freshness | When queried | When called | Last successful refresh | Captured inserts reflected; corrections missed |
| Read cost | Joins and aggregates each time | Same work each call | Reads stored totals | Reads stored totals |
| Extra write cost | None for reporting | None for reporting | Recalculation during refresh | Summary write for each captured insert |
| Side effects | Read-only | Read-only | Refresh replaces stored results | Payment insert also changes summary |
| Recovery | Rerun query | Call function | Refresh from base records | Rebuild from base records |
| Operational complexity | Maintain query | Maintain shared function | Schedule and monitor refreshes | Handle corrections, failures and rebuilds |

These describe the work involved. Not measured performance.

### Side-effect trace: captured payment insert

For the experiment inserting PAY-CASE-CAPTURED:

1. PostgreSQL checks required values, CHECK constraints,
   uniqueness and foreign-key references.
2. The AFTER INSERT trigger runs for the new payment.
3. It finds OP-METRO through ticket → trip → route.
4. It updates the operator/date summary from 36 / 1 to 72 / 2.
5. The payment and summary writes commit in the same transaction.
   A trigger failure prevents the payment insert from committing.
6. Direct queries and the function reflect the payment when it
   becomes visible to their transaction. The trigger summary
   changes with the payment. The materialized view stays unchanged
   until refreshed.

AFTER INSERT means after inserting the row, not after committing.

The summary update locks its affected row during the transaction.
Writes to the same summary row may wait. This is expected behaviour;
lock waiting was not measured in this experiment.

The application can observe success or failure of its payment
statement, although that statement also causes a summary write.

### Issue register: incorrect trigger summary

- Evidence: after the refund, the direct query and function
  returned 86 / 2, while the trigger summary returned 72 / 2.
  After deletion, the base total was 36 / 1 but the summary
  remained 72 / 2.
- Cause: the supplied trigger handles INSERT only. It misses
  changes to existing payments and their deletion.
- Consequence: operators can receive incorrect revenue reports.
- Immediate recovery: rebuild the affected total from payments,
  with no concurrent payment writes during this lab's repair.
- Decision: use the direct query for the current implementation.
  Repairing the summary does not repair its trigger logic.
- Open question: if we retained the summary, how would every
  relevant correction and operator reassignment be handled?

### When I would reconsider the recommendation

If repeated calculations become expensive, I would measure the
cost and consider a materialized view because reporting tolerates
some delay.

It would need a refresh schedule, failure monitoring and a visible
last-successful-refresh time. Refresh duration and failures must
be considered when setting a maximum acceptable age.

A SQL function remains useful for sharing one calculation across
multiple callers without introducing stored reporting totals.

## Lecture 4 — Product-identity migration

### Task

Change tickets from referencing products by code to referencing
a stable product ID. Preserve existing product relationships,
ticket prices and currencies while supporting a transition
between old and new application code.

I kept the work on main rather than creating a separate branch.

### What I learned

- Adding a column does not populate its relationship automatically.
- Expanding first allows old and new code to coexist.
- A fallback reader can resolve a product without changing the ticket.
- Backfill fills missing references using the original product code.
- An idempotent backfill can safely run again and catch late old writes.
- Separate foreign keys do not guarantee that code and ID identify
  the same product.
- Making the ID required breaks old-only writers.
- Removing the old column also breaks readers that still mention it.
- A query can run successfully yet omit tickets, as an ID-only
  inner join does before all references are populated.

### Implementation and assumptions

Migrations 030–032 expand the schema, backfill ticket references,
validate the foreign key and require product_id.

Products keep their business-facing code. Product codes must remain
unique and must not be renamed or reused during the overlap period.

The writer derives product_code from the supplied product ID.
It does not accept an independently chosen product code.
Ticket price and currency remain agreed purchase inputs.

### Evidence: migration stages

| Experiment | Observed result |
|---|---|
| Remove old reference too early | Old reader failed with 42703; rollback restored data |
| Expand schema | Products received UUIDs; existing tickets' product_id values remained null |
| Old writer after expansion | Succeeded without supplying product_id |
| Fallback reader | Resolved all tickets before backfill |
| New writer | Stored matching DAY code/ID and agreed price of 65 DKK |
| Direct mismatched pair | Database accepted it; verification detected it; rolled back |
| First backfill | Updated four tickets |
| Second backfill | Updated zero tickets |
| Late old write | Created another missing ID; backfill updated one row |
| Require ID with a missing value | Failed with 23502; rolled back |
| Require ID after verification | Succeeded |
| Old writer after ID became required | Failed with 23502 |
| Remove old column without CASCADE | Succeeded inside a reversible rehearsal |
| Final reader and writer | Worked without tickets.product_code |
| Unknown product supplied to new writer | Rejected with 23502 |

### Evidence: historical values

The original tickets retained these products and prices:

| Ticket | Product | Price | Currency |
|---|---|---:|---|
| TICKET-1 | SINGLE | 36.00 | DKK |
| TICKET-2 | SINGLE | 36.00 | DKK |
| TICKET-3 | DAY | 65.00 | DKK |

The DAY catalogue price was 80 DKK. The migration preserved
the ticket's historical price of 65 DKK.

### Compatibility matrix

| Reader/writer | Before expansion | Expanded, before backfill | ID required | Old column removed |
|---|---|---|---|---|
| Old reader | Works | Works | Works | Fails |
| Old writer | Works | Works | Fails | Fails |
| Fallback reader | Fails | Works | Works | Fails |
| Dual-reference writer | Fails | Works | Works | Fails |
| ID-only reader | Fails | Omits tickets without IDs | Works | Works |
| ID-only writer | Fails | Fails | Fails | Works |

The ID-only writer fails while the old code column still requires
a value. Its deployment must be coordinated with schema changes.

### Design decision: rollout and rollback

I would remove the old ticket-code column promptly after confirming
that all readers and writers use IDs, dependencies are checked,
and data verification passes.

Old application instances must stop before removal. Their inserts
would otherwise fail immediately; a cleanup script cannot repair
a purchase that was never stored.

After requiring product_id, returning to the old application would
require relaxing that requirement, retaining valid product codes,
and resuming backfill. After dropping the old column, it would also
require reconstructing the column and references.

### Current database state

Product IDs are required. The old ticket-code column still exists
because the removal rehearsal was rolled back. Six tickets remain.
Final verification found no reference problems or changes to
the original tickets' products, prices or currencies.

### Limitations and operational considerations

- A UUID default generates IDs; it does not make them immutable.
  Application code should never reassign product IDs. Production
  permissions should prevent the application role from updating
  that column. This permission policy is not implemented here.
- A three-second lock timeout limits lock waiting, not total runtime.
  Larger tables require assessment of update volume, validation
  scans, locking and deployment timing.
- Repository inspection cannot prove which application versions
  are running. Deployment verification is necessary.
### Evidence: conflicting caller input

The extra supplied_product_code input was SINGLE while the selected
product ID belonged to DAY. The writer ignored the extra input and
derived DAY from the ID. The inserted test ticket was rolled back.
This protects writes made through this script; it does not enforce
matching code/ID pairs for arbitrary SQL writers.

### What I learned: migration tools

The [AI-drafted EF Core comparison](lecture04/ef-core-comparison.md)
expresses the same expansion, backfill and required-ID stages.
It is a comparison draft, not generated or executed EF output.

A migration tool can express columns, keys and nullability, but it
cannot infer the correct historical mapping, preserve purchase prices
by intent, or determine when old application instances have stopped.
Those decisions and the explicit backfill still require domain knowledge.

History-based idempotent deployment is different from repeatable data
backfill: an already-applied migration may be skipped even if an old
writer has since created another ticket with a missing ID.

### Evidence files

The [Lecture 4 evidence index](lecture04/README.md) links the
SQL scripts, execution commands and recorded successes and failures.
All personal learning notes and rollout decisions remain in this file.

