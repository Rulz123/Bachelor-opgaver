# Lecture 3: reporting programmability

## Business definition

Captured revenue is the sum of `payments.amount` grouped by the UTC calendar
date of `payments.created_utc`, currency, and `payments.status = 'Captured'`.
Refunded and failed payments are excluded. No refund table exists, so a
payment changed to `Refunded` is excluded from the next calculation. Deleting a
payment removes it from the direct query and trigger summary; a materialized
view remains stale until refreshed. Amounts are never combined across
currencies.

## Four strategies

1. `database/postgres/queries/lecture03_revenue.sql` is the direct query.
2. `captured_revenue_for_day(date, currency)` is the PostgreSQL function.
3. `captured_revenue_daily_mv` is the materialized view and requires
   `refresh materialized view captured_revenue_daily_mv`.
4. `captured_revenue_daily` is maintained by
   `payments_captured_revenue_summary`, which handles payment insert, update,
   and delete transitions.

The trigger is an explicit write-side effect. It is rebuildable by truncating
and repopulating the summary from captured payments in a controlled operation;
it adds write cost and must be considered under concurrent payment updates.

## Comparison

| Strategy | Correctness | Freshness | Read cost | Write cost | Hidden effects | Rebuildability | Workload fit |
|---|---|---|---|---|---|---|---|
| Direct query | Current source rows | Immediate | Higher | None | None | Excellent | Good for occasional reports |
| Function | Current source rows | Immediate | Query cost | None | None | Excellent | Good reusable API |
| Materialized view | Correct after refresh | Refresh-dependent | Low | Refresh cost | Explicit operational step | Excellent | Good for scheduled reports |
| Trigger summary | Immediate when trigger succeeds | Immediate | Low | Higher per payment | Trigger writes another table | Good with rebuild script | Good for frequent dashboards |

For this workload, keep the direct query as the correctness reference and use
the trigger summary only when low-latency repeated reads justify its write and
operational complexity. Use the materialized view for deliberately stale,
cheap reporting windows.

Run `database/postgres/experiments/lecture03_reporting.sql` after applying
the base schema and migration `020_reporting_objects.sql`. The experiment
prints a `72.00` baseline for all four strategies, then inserts a temporary
`10.00` captured payment. While it exists, the direct query, function, and
trigger summary report `82.00`, but the materialized view remains stale at
`72.00`. Refreshing the materialized view at that point changes it to `82.00`.
The experiment then deletes the temporary payment, shows the trigger summary
back at `72.00`, refreshes the materialized view again, and shows the restored
`72.00` baseline before rolling back the transaction.