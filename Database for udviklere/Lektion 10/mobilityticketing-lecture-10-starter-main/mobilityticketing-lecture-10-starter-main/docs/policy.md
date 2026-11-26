# Replication policy

Live verification is now recorded in [the local report](../../../../validation/README.md):
7 supplied, 12 package and 13 replica/outage checks passed. Run
`COMPOSE_PROJECT_NAME=dbdverify-l10-20261126 node scripts/live.test.cjs` from this lesson.
The original verification limitation at the end of this note describes the package review.

`compareChange` only classifies a successful read: null means AbsentAtVerification; four equal fields mean PresentMatches; otherwise PresentConflicts. It never retries, queries or writes. The runner owns failed-read classification. Absence is not proof that an earlier timed-out write can never appear.

| Operation | Policy | Outage behaviour |
|---|---|---|
| Journey search | Secondary reads can favour availability; label uncertain freshness. | Show available data with freshness caveat or an explicit temporary error. Never confirm a booking from cached/secondary availability. |
| Ticket purchase | Stable operation ID; primary and majority acknowledgement. | Timeout means outcome uncertain. Verify the same ID after recovery before retrying the same logical change. |
| Reporting | Record source version and cutoff; majority reads when required. | Delay publication if the required state cannot be established. |

Adding replicas may help read availability and capacity, but does not partition writes. City sharding can leave the busy city as a hot shard and complicates cross-city searches. Measure latency, throughput, skew and replication delay before choosing.

## Verification

Run `node scripts/verify.test.cjs` for the pure comparison checks. Real majority/secondary/outage experiments still require Docker and the commands in README. No replica-set measurements were produced in this workspace; fill docs/evidence.md only with actual runner JSON.
