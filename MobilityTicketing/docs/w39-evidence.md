# W39 evidence matrix

Statuses mean: `Proven` is supported by repository content and executed
verification; `Partial` has implementation but incomplete demonstration;
`Missing` has no adequate implementation; `Unable to verify` could not be
checked in the available environment.

| Requirement | Status | Evidence and executable verification | Remaining evidence |
|---|---|---|---|
| Lecture 1 workload map | Proven | `docs/dossier.md` access-pattern map | None |
| Lecture 1 relational model and schema | Proven | `docs/dossier.md` ER diagram; `database/postgres/init/001_relational_baseline.sql` | None |
| Lecture 1 deterministic seeds | Proven | `database/postgres/init/002_seed.sql`, `011_ticketing_seed.sql`; clean replay | None |
| Upcoming trips query | Proven | `database/postgres/003_queries.sql`; replay returned four LINE-M2 trips | None |
| Ordered stops query | Proven | `database/postgres/003_queries.sql`; replay returned ordered stop positions | None |
| Routes with zero trip counts | Proven | `003_queries.sql` uses `left join` and filters date in `on`; replay verified route preservation | None |
| Route-stop modelling choice and assumption | Proven | `docs/dossier.md`, `docs/lab.md`, `database/postgres/init/001_relational_baseline.sql` use `(route_id, stop_sequence)` | Explain during review how ordering supports repeated stops |
| Lecture 2 integrity map | Proven | `docs/lecture02.md` catalogue and deferred-boundary table | None |
| Lecture 2 successful and rejected writes | Proven | `database/postgres/experiments/lecture02_constraints.sql`; explicit valid `UPDATE`/`INSERT` plus expected named failures after contract | None |
| Lecture 2 enforcement boundary | Proven | `docs/lecture02.md`: concurrent reservations and external payment capture are deferred | None |
| Lecture 3 base query/function | Proven | `database/postgres/queries/lecture03_revenue.sql`, `020_reporting_objects.sql` | None |
| Lecture 3 stale materialized view and correction | Proven | `database/postgres/experiments/lecture03_reporting.sql`: 72.00 baseline; 82.00 direct/function/trigger versus stale 72.00 MV; refresh to 82.00; cleanup and refresh to 72.00 | None |
| Lecture 3 responsibility decision | Proven | `docs/lecture03.md` comparison and recommendation | None |
| Lecture 4 expand/backfill/contract | Proven | `030`, `031`, `032` migrations; clean replay | None |
| Lecture 4 old/new readers and writers | Proven | `database/postgres/experiments/lecture04_compatibility.sql`; temporary isolated overlap | None |
| Lecture 4 late old-writer and subsequent backfill | Proven | Same experiment inserts `COMPAT-LATE-OLD-WRITER`, then resolves it | None |
| Lecture 4 price/currency preservation | Proven | Compatibility experiment compares ticket and product values; clean replay kept DKK prices | None |
| Lecture 4 dependency inspection and no CASCADE | Proven | Compatibility experiment queries catalog; `030`-`032` contain no `CASCADE` | None |