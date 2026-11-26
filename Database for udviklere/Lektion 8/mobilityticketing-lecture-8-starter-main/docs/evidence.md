# Query optimization: audited measurements

## New local run

[The 26 November live report](../../../validation/README.md) contains fresh correctness
checks, all 40 measured plans, index inventory and partition results. The partial composite
index was actually used for both routes: median 0.241 ms (R-7) and 0.344 ms (R-0).
The older measurements below remain historical and are not evidence of that index.

The numbers below were extracted from the existing committed logs on branch `Nichlas`; they were not rerun during this review. Five measured executions per file, excluding warm-up.

| Route | File | Median ms | Range ms | Observed access |
|---|---|---:|---|---|
| R7 | [baseline-r7.txt](baseline-r7.txt) | 7.365 | 6.816–7.545 | sequential scan + sort |
| R0 | [baseline-r0.txt](baseline-r0.txt) | 14.196 | 13.718–14.820 | sequential scan + sort |
| R7 | [rewrite-r7.txt](rewrite-r7.txt) | 8.775 | 8.641–8.830 | sequential scan + sort |
| R0 | [rewrite-r0.txt](rewrite-r0.txt) | 8.998 | 8.779–9.059 | sequential scan + sort |
| R7 | [route-only-r7.txt](route-only-r7.txt) | 1.878 | 1.838–2.143 | route-only bitmap index + sort |
| R0 | [route-only-r0.txt](route-only-r0.txt) | 11.779 | 9.988–13.851 | sequential scan + sort |
| R7 | [composite-r7.txt](composite-r7.txt) | 1.979 | 1.824–2.195 | route-only bitmap index + sort |
| R0 | [composite-r0.txt](composite-r0.txt) | 9.514 | 9.120–11.358 | sequential scan + sort |
| R7 | [real-composite-r7.txt](real-composite-r7.txt) | 1.747 | 1.728–2.722 | route-only bitmap index + sort |
| R0 | [real-composite-r0.txt](real-composite-r0.txt) | 8.697 | 8.596–8.875 | sequential scan + sort |

## What the logs actually establish

**The files named composite and real-composite do not demonstrate a composite index.** All three index inventory files show only `index_trips_route` (832 kB) plus `trips_pkey` (2640 kB). Their R-7 plans use the route-only index; their R-0 plans use a sequential scan. Filenames are not evidence of active indexes.

The rewrite improves the recorded busy-route R-0 median from 14.196 ms to 8.998 ms, but the R-7 rewrite alone is slower than its baseline. The route-only index helps R-7; it does not make the planner use it for the dominant R-0 route. Runs were taken sequentially without controlled hardware/cache conditions, so small differences should not be overinterpreted.

## Candidate to verify next

`queries/indexes.sql` defines a partial B-tree on `(route_id, scheduled_departure_utc, id) WHERE status = 'Scheduled'`. This matches equality, range and ordering. Keep it only after checking its actual definition, size, both route plans and result equivalence. Inserts/reschedules/cancellations have index-maintenance cost. The new candidate query uses explicit UTC bounds, independent of session timezone. Old measurements predate this correction and must be rerun.

Use `docker compose exec -T postgres bash /lab/run.sh check`; then apply the index file using psql, inspect `pg_indexes`, and repeat all five measurements per route as described in README. Check both the first 20 rows and the complete day. No successful correctness-check output was found in these timing logs.

## Partitioning

See [partition-plans.txt](partition-plans.txt). The October report accesses only October in the partitioned layout; ticket history has no date predicate and must consider all months. Monthly partitioning therefore fits time-based retention/reporting better than undated ticket-support lookups. Partitioning is not automatically faster.

## AI review

Prompt: audit active indexes against the five-run logs and check UTC boundaries. Accepted corrections: distinguish the route-only index from the intended composite index, and make the UTC boundary explicit. Performance of the intended composite index remains unverified.
