# Lectures 1-4 audit

This audit uses only repository content and commands that were actually run. A
static `Proven` result means the required implementation is present in the
repository; runtime database behavior is separately marked where it has not yet
been executed.

| Lecture | Requirement area | Status | Evidence | Remaining work |
|---|---|---|---|---|
| 1 | Relational entities, keys, relationships, seeds, and workload queries | Proven | `database/postgres/init/001_relational_baseline.sql`, `002_seed.sql`, `003_queries.sql`, `docs/lab.md` | No remaining verification gap in the replay. |
| 2 | Named capacity, ticket, price, payment, validation, status, and currency constraints | Proven | `database/postgres/migrations/011_ticketing_integrity.sql` through `018_supported_currency.sql` | Deferred transaction/external rules are documented in `docs/lecture02.md`. |
| 2 | Negative INSERT and UPDATE verification | Proven | `database/postgres/experiments/lecture02_constraints.sql` | Existing legacy experiment remains separately executable before product contract; new post-contract checks are verified. |
| 3 | Direct revenue query | Proven | `database/postgres/queries/lecture03_revenue.sql` | No remaining verification gap in the replay. |
| 3 | Function, materialized view, and trigger summary | Proven | `database/postgres/migrations/020_reporting_objects.sql` | Transition and refresh behavior verified. |
| 3 | Scenario evidence and comparison | Proven | `database/postgres/experiments/lecture03_reporting.sql`, `docs/lecture03.md` | Transition output verified on PostgreSQL; duplicate reference rejection observed. |
| 4 | Expand product identity | Proven | `database/postgres/migrations/030_expand_product_identity.sql`, `database/postgres/experiments/lecture04_product_identity.sql` | Compatibility state verified in temporary tables and clean replay. |
| 4 | Idempotent backfill | Proven | `database/postgres/migrations/031_backfill_ticket_product.sql`, `lecture04_product_identity.sql` | Second pass updated 0 rows and left 0 unresolved rows. |
| 4 | Contract and deliberate NOT NULL failure | Proven | `database/postgres/migrations/032_contract_product_identity.sql`, `lecture04_product_identity.sql` | Expected NOT NULL failure and final contract state verified. |
| 4 | ORM comparison | Proven by documented comparison | `docs/lecture04.md` contains an explicitly non-executed EF-style migration and comparison matrix. | No EF Core runtime is present or required. |