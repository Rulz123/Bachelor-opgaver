# Report workflow

Current evidence: [14 pure and 14 live checks passed](../../../../validation/README.md).
`COMPOSE_PROJECT_NAME=dbdverify-l11-20261126 node tests/live.test.cjs` builds the CLI,
tests actual PostgreSQL/S3 adapters and restores services after controlled outages.
The original validation-scope paragraph below describes the earlier package-only review.

The CLI calls application-owned ports. Infrastructure implements S3 version reads, PostgreSQL catalogue access and local receipts. PostgreSQL owns validation rows and committed report metadata; object storage owns export bytes; receipts own pending-upload recovery state.

Stage hashes and counts the exact bytes, reserves the ID before PUT, then records bucket/key/version and expectations. Publish stages and recovers. Recover verifies the recorded exact version before saving metadata and never uploads again. Publication occurs only when the catalogue commits. Verify resolves the catalogue ID first; the latest object at a key is never substituted for the recorded version.

## Retry boundaries

- PUT failure may be ambiguous: retain the reservation and investigate the store. Do not blindly stage again.
- PUT succeeded and receipt exists, but catalogue failed: recover from that receipt without PUT.
- Crash after PUT before receipt: an empty reservation signals uncertain outcome. Reconcile versions before further upload.
- Repeated identical recovery delegates idempotency to catalogue SaveAsync. Changed predecessor or metadata must conflict there.
- Object-store read failure becomes ReadFailed, not Mismatch. Catalogue failure becomes CatalogueReadFailed. Unknown IDs are UnknownReport.

## Access and retention

An API authorizes report access before returning its metadata or opening its object. The storage identity needs only destination upload and exact-version read access; runtime verification does not require version deletion. Cleanup separately reconciles published catalogue references and pending receipts/reservations. Never delete a version referenced by a retained report. A checksum detects content changes but cannot restore deleted bytes.

## Validation scope

`dotnet run --project tests/WorkflowChecks` runs 14 checks with in-memory adapters, including exact-version reads, interrupted publication, retry without reupload, corrupt receipts and differentiated outages. This verifies orchestration, not real PostgreSQL/S3 adapter behaviour. The live experiments in README and Moodle remain necessary; no live results are invented here.
