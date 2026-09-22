# MobilityTicketing: Lectures 1-4

This repository is one cumulative PostgreSQL project for the first four
lectures. It models route maintenance, scheduled trips, ticket products,
users, tickets, payments, and validations, then adds database integrity,
reporting programmability, and a rolling product-identity migration.

The transactional tables remain the source of truth. Reporting strategies and
migration experiments are kept separate from setup files so their behavior can
be compared and verified explicitly.

## Start the database

Requirements:

- Docker Desktop with Compose

Start PostgreSQL:

```bash
docker compose up -d
```

The database is available at `localhost:5432` with database `mobility`, user `mobility`, and password `mobility`.

To stop it:

```bash
docker compose down
```

The PostgreSQL entrypoint only executes files directly under its init directory;
it does not recurse into this repository's `init/`, `migrations/`, or
`experiments/` folders. Apply files explicitly in dependency order:

```bash
for file in \
	database/postgres/init/001_relational_baseline.sql \
	database/postgres/init/002_seed.sql \
	database/postgres/init/010_ticketing_draft.sql \
	database/postgres/init/011_ticketing_seed.sql \
	database/postgres/init/012_legacy_trip_capacity.sql \
	database/postgres/migrations/011_ticketing_integrity.sql \
	database/postgres/migrations/012_ticket_references.sql \
	database/postgres/migrations/013_ticket_identity_and_validity.sql \
	database/postgres/migrations/014_prices_and_currency.sql \
	database/postgres/migrations/015_payment_references.sql \
	database/postgres/migrations/016_validation_ticket_identity.sql \
	database/postgres/migrations/017_status_integrity.sql \
	database/postgres/migrations/018_supported_currency.sql \
	database/postgres/migrations/020_reporting_objects.sql \
	database/postgres/migrations/030_expand_product_identity.sql \
	database/postgres/migrations/031_backfill_ticket_product.sql
do
	docker compose exec -T postgres psql -U mobility -d mobility \
		-v ON_ERROR_STOP=1 -f "/docker-entrypoint-initdb.d/${file#database/postgres/}"
done
```

The contract migration `032_contract_product_identity.sql` is a later
deployment and must only be applied after compatibility readers/writers have
been retired and catalog dependencies have been checked. Do not use a volume
reset casually; `docker compose down -v` deletes this project's database data.

## Verification

Lecture 1 workload queries are in `database/postgres/003_queries.sql`. Run
them with representative parameters and record results. Expected-success
verification uses `-v ON_ERROR_STOP=1`.

The original Lecture 2 negative checks are in
`database/postgres/experiments/constraints_should_fail.sql`; run them
separately against the pre-contract schema. The maintained post-contract
checks are in `database/postgres/experiments/lecture02_constraints.sql`.
PostgreSQL rejection by a named constraint is expected evidence, not an
unexpected test failure. The complete invariant ownership catalogue is in
`docs/lecture02.md`.

Lecture 3 reporting definitions and comparison are documented in
`docs/lecture03.md`; run `database/postgres/experiments/lecture03_reporting.sql`
after migration `020_reporting_objects.sql`.

Lecture 4 product identity is documented in `docs/lecture04.md`; run
`database/postgres/experiments/lecture04_product_identity.sql` for the isolated
expand/backfill/NOT NULL-failure evidence. Apply `032_contract_product_identity.sql`
only after its stated preconditions are satisfied.

The complete static audit is in `docs/lecture-audit.md`.

The original Lecture 1 slice intentionally excludes MongoDB, Redis, queues,
payment logic, validation logic, reporting tables, and performance indexes.
Later lecture work in this same repository adds the ticketing, reporting, and
identity-migration objects documented above.

## Project structure

- `compose.yaml`: PostgreSQL 17 infrastructure.
- `database/postgres/init/`: baseline schema and deterministic seed files.
- `database/postgres/migrations/`: ordered integrity, reporting, and product-identity changes.
- `database/postgres/queries/`: reusable reporting queries.
- `database/postgres/experiments/`: expected failures and isolated behavior demonstrations.
- `docs/`: domain model, lab brief, audit, and Lecture 3-4 design notes.

# Compulsory Assignment 1 review guide

Submitted commit: See the exact immutable commit hash submitted in Moodle.

Setup and reset instructions: [README setup and verification](#start-the-database)

Full evidence matrix: [W39 evidence matrix](docs/w39-evidence.md)

## Where to find the work

Lecture 1: model, workload map and queries: [dossier](docs/dossier.md), [lab brief](docs/lab.md), [queries](database/postgres/003_queries.sql)

Lecture 2: constraints and tests: [integrity catalogue](docs/lecture02.md), [post-contract tests](database/postgres/experiments/lecture02_constraints.sql)

Lecture 3: reporting experiment and comparison: [reporting guide](docs/lecture03.md), [experiment](database/postgres/experiments/lecture03_reporting.sql), [query](database/postgres/queries/lecture03_revenue.sql)

Lecture 4: migration stages and verification: [migration guide](docs/lecture04.md), [compatibility experiment](database/postgres/experiments/lecture04_compatibility.sql), [migration experiment](database/postgres/experiments/lecture04_product_identity.sql)

## Two decisions worth discussing

### Decision 1

What did we choose? We identify a route stop by `(route_id, stop_sequence)`, not `(route_id, stop_id)`.

What was the alternative? Treating each stop ID as unique within a route.

Why does our choice fit MobilityTicketing? A route may visit the same stop more than once, while sequence gives deterministic ordering for timetable and route display queries.

Which file or result supports it? [ER model and schema](docs/dossier.md) and [ordered-stops query](database/postgres/003_queries.sql).

### Decision 2

What did we choose? We compare direct SQL, a function, a materialized view, and a trigger-maintained summary for captured revenue.

What was the alternative? Hiding reporting responsibility in one unexamined reporting table or always calculating at read time.

Why does our choice fit MobilityTicketing? The direct query is the correctness reference; materialized views suit scheduled reports, while the trigger summary suits frequent low-latency reads when its write cost is justified.

Which file or result supports it? [reporting comparison](docs/lecture03.md) and [staleness experiment](database/postgres/experiments/lecture03_reporting.sql).

## One limitation or open question

What does the implementation not guarantee? PostgreSQL constraints cannot make an external payment operation and a PostgreSQL transaction atomically consistent.

Which evidence documents the boundary? [integrity catalogue](docs/lecture02.md).

What should be checked or implemented next? Add an integration reconciliation and idempotency design for the external payment provider, while retaining database constraints for local invariants.
