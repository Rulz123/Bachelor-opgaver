# Actual local validation: 26 November 2026

Branch: `work/josephine-dbd-complete`, base `origin/Nichlas` at `d743bb3`.
Latest lecture 7 run: **51 passed, 0 failed/skipped**, after 10 focused snapshot passes.
The full suite passed again before commit/push on 26 November at 12:40 UTC.
The incoming DTO-as-entity mapping caused 42 failures; SnapshotDocument JSONB storage,
per-ID locking, retry/conflict checks and rollback fixed them without changing tests.
No Moodle submission was made.

## Evidence archive

[evidence-2026-11-26.tar.gz](evidence-2026-11-26.tar.gz) contains all 45 raw logs/JSON/TRX
files and the original detailed README, including earlier failures and measurements.
The archive was compared against every original before loose files were removed.
To read the latest full-suite log without extracting files:

```bash
tar -xOzf "$DBD_ROOT/validation/evidence-2026-11-26.tar.gz" dbd-l07-snapshot-fix-full.log
```

Set `DBD_ROOT` using the helper below. Archive member `README.md` preserves the full
history; `lecture07-snapshot-fix-full.trx` is the latest complete test result.

## Isolation

Only `dbdverify-lNN-20261126` Compose projects were reset; no existing user data or
volumes were deleted. Lecture 7 tests use `mobility_l07_test`; lecture 4 creates
`dbdverify_l04_*`; lecture 11 retains receipts in `/tmp/dbd-l11-receipts-*`.
Docker 29.8.1, Compose v5.5.1; lecture 7 uses SDK 8.0.426 and PostgreSQL 17.11.
All course .NET projects built successfully.
At verification end, lessons 6-11 were running and 1-5 stopped. Host ports:
lecture 7: 5432; lecture 6: 27017/6379; lecture 11: 127.0.0.1:25411/19011.

## Results

Counts are distinct checks, not repeated executions. Expected rejected writes and
outages are successful negative checks, not successful business operations.

| Lesson | Changes | Automated checks | Final status | Actual execution | Remaining scope |
|---|---|---:|---|---|---|
| 1 | Fix seed column/IDs/order and 3 parameterized queries | 0 | Smoke passed | Fresh init; repeat seed added 0; 24 trips, 12 stops; 3 queries ran | No assertion suite; wider ER/access-map/dossier deliverables not verified |
| 2 | Select original init DDL; integrity migration and SQLSTATE tests | 12 | 12 passed | Invalid writes rejected; valid payment accepted; tests roll back | Cross-row capacity, external payment, retention are workflow policies |
| 3 | Restore insert-only trigger; repeatable reporting checks | 8 | 8 passed | All 4 reporting methods compared after 6 mutations and refresh | Summary deliberately stale after corrections; not a production solution |
| 4 | Repair migrations, readers/writers and pair FK; add tests | 13 | 13 passed | Expansion, backfill, conflicts, historical prices, ID-only access | Actual retirement of old application versions and ID permissions are operational |
| 5 | No code changes | 10 | 10 passed | Search, A/B/C updates, alternative model, BSON growth, invalid-document rejection | Demonstrations are not an atomic cross-document update workflow |
| 6 | Embedded cancellation fix; live runner | 14 | 14 passed | Cache hits/expiry/corruption/cancellation, real outages/recovery, concurrency and Lua | Cold batch made 10 Mongo reads; no request coalescing; cache is not booking authority |
| 7 | Booking/snapshot patch; unseal snapshot entity for EF baseline | 51 | 51 passed, 0 failed/skipped | Whole suite on PostgreSQL; separate CLI processes; small/100k-trip measurements | No remaining suite failure; not production load certification |
| 8 | UTC query correction | 4 | 4 passed before/after index | 40 measured EXPLAIN ANALYZE plans, index inventory, partition comparison | Local sequential timings, not controlled production benchmarks |
| 9 | Parallel-session checks | 13 SQL assertions + 6 concurrent | All passed | Idempotency, last seats, conflicts, rollback, cancellation | Deterministic deadlock, explicit lock variants and stronger-isolation retries not rerun |
| 10 | Verifier patch; replica integration checks | 7 supplied + 12 package + 13 live | 32 passed | Reads, failover, w=3 timeout, no primary, recovery | All members share one host; no host-failure claim |
| 11 | Workflow/Infrastructure patch; live checks | 14 workflow + 14 live | 28 passed | Exact versions, corrupt receipt, predecessor rollback, retries, both outages | Upload/receipt crash gap and retention need operational policy |

## Measurements

### Lecture 7

| Fixture/read | Executed reads | Route entities | Trip entities |
|---|---:|---:|---:|
| 4 routes / 9 trips, lazy baseline | 5 | 4 | 9 |
| 4 routes / 9 trips, live projection | 1 | 0 | 0 |
| Published snapshot read | 1 | 0 | 0 |
| 200 routes / 100,000 trips, lazy baseline | 201 | 200 | 100000 |
| 200 routes / 100,000 trips, live projection | 1 | 0 | 0 |

### Lecture 8

Five measured plans per row after an unrecorded warm-up. Times are PostgreSQL
Execution Time in milliseconds, not Docker wall time. Other work ran on this laptop.

| Route | Query/index | Median | Min-max |
|---|---|---:|---|
| R-7 | Baseline / PK | 38.878 | 30.922-43.113 |
| R-0 | Baseline / PK | 56.874 | 49.004-110.076 |
| R-7 | Rewrite / PK | 24.482 | 21.973-32.454 |
| R-0 | Rewrite / PK | 31.917 | 20.450-39.530 |
| R-7 | Rewrite / route-only | 4.824 | 3.471-5.907 |
| R-0 | Rewrite / route-only | 30.610 | 22.761-33.109 |
| R-7 | Rewrite / partial composite | 0.241 | 0.228-0.348 |
| R-0 | Rewrite / partial composite | 0.344 | 0.227-0.377 |

Both final plans used `index_trips_route_departure_status`:
`(route_id, scheduled_departure_utc, id) WHERE status='Scheduled'`, size 4408 kB.
Correctness checks matched ordered 20-row pages and 38/991-row days.

## Exact commands

Define this Bash helper once. It only selects a directory and our isolated project:

```bash
DBD_ROOT='/home/josephine/Bachelor-opgaver/Database for udviklere'
dbd() {
    local number="$1"
    local folder="$DBD_ROOT/Lektion $number/mobilityticketing-lecture-$number-starter-main"
    if [[ "$number" != 7 && "$number" != 8 ]]; then
        folder="$folder/mobilityticketing-lecture-$number-starter-main"
    fi
    cd "$folder" || return
    printf -v COMPOSE_PROJECT_NAME 'dbdverify-l%02d-20261126' "$number"
    export COMPOSE_PROJECT_NAME
}
```

Lessons 1-5/7 compete for host port 5432. Before switching between them, run
`docker compose stop postgres` in the preceding **dbdverify** project. Before switching
5/6, stop its `mongo` similarly. Never stop unrelated services. Use `stop`, not `down -v`.
These commands resume retained test environments; fresh environments need the one-time
migrations indicated below. Fresh project suffixes give separate volumes/containers.

### Lecture 1

```bash
dbd 1
docker compose up -d postgres
docker compose exec -T postgres pg_isready -U mobility -d mobility
docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/002_seed.sql
docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 -v route_id=LINE-M2 -v after_utc=2000-01-01T00:00:00Z -v service_date=2026-11-26 -f /docker-entrypoint-initdb.d/003_queries.sql.example
```

Local status: `'1'` scheduled, `'0'` cancelled.

### Lecture 2

```bash
dbd 2
docker compose up -d --wait postgres
# Fresh environment only; already installed in the retained project:
# docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 < database/postgres/migrations/011_ticketing_integrity.sql
docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 < database/postgres/experiments/verify.sql
```

Use `verify.sql`; the old `constraints_should_fail.sql` contains pasted output, not a runnable suite.

### Lecture 3

```bash
dbd 3
docker compose up -d --wait postgres
# Fresh environment only, once and in order:
# for file in database/postgres/migrations/*.example; do
#   docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 < "$file" || break
# done
docker compose exec -T postgres psql -X -U mobility -d mobility -v ON_ERROR_STOP=1 < database/postgres/experiments/reporting_cases.sql
```

Checks assert the reporting methods' differences and roll back experiment writes.

### Lecture 4

```bash
dbd 4
node database/postgres/experiments/lecture04/verify.cjs
```

Starts Compose and checks all migration phases in a new retained test database.

### Lecture 5

```bash
dbd 5
docker compose up -d --wait postgres mongo
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/reset.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/search.test.js
for scenario in A B C; do
    docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/reset.js
    docker compose exec -T mongo mongosh --quiet --eval "load('/scripts/lecture05/search.js'); load('/scripts/lecture05/updates_test${scenario}.js');"
done
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/reset.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/alternative.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/alternative_search.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/growth.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/reset.js
docker compose exec -T mongo mongosh --quiet --file /scripts/lecture05/validation.js
# Last command deliberately exits 1: Document failed validation.
```

### Lecture 6

```bash
dbd 6
node database/redis/lecture06/verify.cjs
```

Builds CacheLab and drives its interactive checks, including service outages/recovery.

### Lecture 7

```bash
dbd 7
docker compose up -d --wait postgres
docker compose build app tests
docker compose run --rm tests
docker compose run --rm app reset
docker compose run --rm app baseline
docker compose run --rm app overview
docker compose run --rm app publish morning
docker compose run --rm app book T201 campus-1
docker compose run --rm app book T201 campus-1
docker compose run --rm app cancel T102
docker compose run --rm app rename R1 'City Ring'
docker compose run --rm app book T102 cancelled-1
# Expected exit 2 above; continue:
docker compose run --rm app overview
docker compose run --rm app snapshot morning
docker compose run --rm app publish revised
docker compose run --rm app snapshot revised
docker compose run --rm app reset
docker compose run --rm app scale
docker compose run --rm app baseline
docker compose run --rm app overview
```

New TRX output goes to the ignored `docs/evidence/lecture07/tests` directory.
This run's results are preserved in the evidence archive.

### Lecture 8

```bash
dbd 8
docker compose up -d --wait postgres
docker compose exec -T postgres bash /lab/run.sh reset
docker compose exec -T postgres bash /lab/run.sh check
for route in R-7 R-0; do
    for query in baseline candidate; do
        docker compose exec -T postgres bash /lab/run.sh measure "$query" "$route"
    done
done
docker compose exec -T postgres psql -U mobility -d mobility_l08 -v ON_ERROR_STOP=1 -c 'CREATE INDEX index_trips_route ON lab08.trips(route_id);'
for route in R-7 R-0; do docker compose exec -T postgres bash /lab/run.sh measure candidate "$route"; done
docker compose exec -T postgres bash /lab/run.sh indexes
docker compose exec -T postgres bash /lab/run.sh check
for route in R-7 R-0; do docker compose exec -T postgres bash /lab/run.sh measure candidate "$route"; done
docker compose exec -T postgres bash /lab/run.sh partitions
```

The archive contains all measured plans and reset/check/index/partition output.

### Lecture 9

```bash
dbd 9
docker compose up -d --wait postgres
# Fresh isolated fixture only: docker compose exec -T postgres bash /lab/run.sh reset
docker compose exec -T postgres bash /lab/run.sh load
docker compose exec -T postgres bash /lab/run.sh check
node sql/concurrency.test.cjs
docker compose exec -T postgres bash /lab/run.sh inspect
```

### Lecture 10

```bash
dbd 10
bash run.sh up
bash run.sh check
node scripts/verify.test.cjs
node scripts/live.test.cjs
bash run.sh status
```

Live tests discover the actual primary, use fresh IDs, and restore members in finally.
No forced replica reconfiguration is performed.

### Lecture 11

```bash
dbd 11
node tests/live.test.cjs
```

Builds and tests the workflow with service recovery. Keep printed receipt directories. Manual commands:

```bash
source ./lab.sh
lab_compose up -d --wait postgres object-store
s3 s3api list-buckets
dotnet run --project tests/WorkflowChecks
dotnet run --project src/MobilityTicketing.Reports -- --help
```

## Not verified

Lecture 9 deterministic deadlocks, explicit lock variants and stronger-isolation retries
were not rerun. External diagrams/dossiers/Notion/Moodle and production reliability were
not certified. Historical "Docker unavailable" notes are superseded by this report.