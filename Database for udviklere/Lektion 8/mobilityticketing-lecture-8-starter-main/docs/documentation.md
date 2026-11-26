
# Lecture 8 – Query optimization evidence

## 1. Baseline departure search

The original departure query uses a date condition that does not
directly express a UTC range.

The baseline execution plans were collected for R-7 and R-0.
R-7 has 38 scheduled departures on 23 October 2026, while
R-0 has 991.

The baseline plans were inspected for:
- Rows scanned.
- Rows removed by filters.
- Sorting operations.
- Execution time.
- Shared buffer hits and reads.

The main optimisation target was reducing the number of rows
that PostgreSQL must examine and avoiding unnecessary sorting.

## 2. UTC range rewrite

The date condition was rewritten using an inclusive lower
boundary and exclusive upper boundary.

This ensures that trips at midnight at the beginning of
23 October are included, while trips at midnight on
24 October are excluded.

The supplied route, status, ordering and limit conditions
were preserved.

The check command passed for both routes, including the
complete-day comparisons.

### Before and after

[Describe the actual differences between the baseline
and rewritten execution plans.]

## 3. Index experiments

### Route-only index

The first candidate was an index on route_id.

This helps PostgreSQL locate trips belonging to a particular
route. However, the database may still need to filter by
departure time and status and sort the results.

[Insert measured results and plan observations.]

### Composite partial index

The second candidate was:

```sql
CREATE INDEX index_trips_route_departure_status
ON lab08.trips
(route_id, scheduled_departure_utc, id)
WHERE status = 'Scheduled';
```

This index supports route filtering, departure-time filtering
and ordering. The partial condition excludes trips that
are not scheduled.

[Insert measured results and plan observations.]

## 4. Index maintenance costs

The composite index requires additional storage.

When a trip is inserted, PostgreSQL must maintain the index
if the trip is scheduled.

When a scheduled departure is rescheduled, the relevant
index entry must be updated.

When a trip is cancelled, it no longer satisfies the partial
index condition, so its index entry must be removed.

The index therefore improves reads at the cost of additional
write operations, storage and maintenance.

## 5. Partitioning

The partitioning experiment compared an ordinary validation
table with a table partitioned by month.

Expected results:
- October: 40,889 validation events.
- Ticket 100: 12 validation events.
- Different ticket-history rows: 0.

### October report

The report filters using the validation timestamp.
PostgreSQL can therefore prune partitions and access only
the October partition.

### Ticket history

The ticket-history query filters using ticket ID instead
of the partition key.

PostgreSQL cannot eliminate the other monthly partitions
using the ticket ID alone. It needs to examine the relevant
September, October and November partitions.

### Partitioning recommendation

Monthly partitioning is useful for monthly reporting and
retention management.

It allows old months to be removed efficiently by dropping
or detaching partitions.

However, it provides less benefit for ticket-history
lookups that need records from multiple months.

## 6. Final recommendation

Index selected:
[Insert chosen index.]

Rejected alternative:
[Insert rejected index and explain why.]

Evidence:
[Compare execution time, buffers, sorting and index size.]

Main trade-off:
The selected index improves departure searches but adds
storage and write-maintenance overhead.

## 7. Conclusion

The experiments demonstrate that query optimisation depends on both query structure and index design.

The original departure query used sequential scans and removed more than 119,000 rows through filtering. Rewriting the date condition as a UTC range made the query more suitable for index usage and improved the R-0 execution time.

The route-only index significantly improved the R-7 search, reducing median execution time from 7.365 ms to 1.878 ms. However, it did not eliminate filtering or sorting, and PostgreSQL continued to use a sequential scan for R-0.

A composite index was therefore investigated to support route filtering, departure-time filtering and ordering while including only scheduled trips.

The partitioning experiment showed that monthly partitioning successfully reduced the October report to the October partition. However, ticket-history queries accessed all three monthly partitions because ticket ID was not the partition key.

Monthly partitioning is therefore more attractive for reporting and historical data retention than for ticket-history lookups in this workload.

The final index recommendation should be based on the measured composite-index results, considering execution time, buffer usage, index size and write-maintenance costs.
