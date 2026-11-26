# Verification status — 26 November 2026

The local Docker build and all 51 PostgreSQL tests now pass (0 failed/skipped).
The first run found three EF proxy failures caused by a sealed SnapshotDocument;
unsealing it fixed the full suite. Builds use the project's pinned SDK 8.0.426.
Separate CLI processes and small/100,000-trip measurements also ran successfully.
See [raw logs, TRX, measurements and isolated restart commands](../../../../../validation/README.md).

Run from this lecture directory:

```sh
docker compose up -d --wait postgres
docker compose build app tests
docker compose run --rm tests
docker compose run --rm app reset
docker compose run --rm app baseline
docker compose run --rm app overview
docker compose run --rm app publish initial
docker compose run --rm app cancel T102
docker compose run --rm app rename R1 "Changed route"
docker compose run --rm app snapshot initial
docker compose run --rm app scale
docker compose run --rm app baseline
docker compose run --rm app overview
```

Use the isolated Compose project described in the linked report. Measured projection:
one read and zero Route/Trip entities at both fixture sizes. Baseline: 5 reads/4 routes/9 trips
for the small fixture; 201 reads/200 routes/100,000 trips for the large fixture.
