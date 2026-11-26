# Cloud Storage: evidence

| Scenario | Prediction | Command, result and exit status | What changed in the catalogue and bucket? |
| --- | --- | --- | --- |
| Publish the original | Store one version, then metadata | publish: Saved, exit 0 | 3-row original published |
| Interrupt after staging a correction | Object/receipt exist; no report yet | stage: version ID, exit 0; verify: UnknownReport, exit 3 | Catalogue count 0 for correction |
| Recover with a mismatched receipt | Reject before publication | recover: Mismatch, exit 1 | Catalogue count remains 0 |
| Recover with a nonexistent predecessor | Reject/roll back metadata | recover: FK violation, exit 1 | Catalogue count remains 0 |
| Recover and repeat recovery | Idempotent; no PUT | both recover calls: Saved, exit 0 | One catalogue row; unchanged version count |
| Verify the original after the correction | Read original exact version | verify original and correction: Matches, exit 0 | Both retained versions readable |
| Verify with object storage stopped | Read failure, not content mismatch | verify: ReadFailed, exit 5 | No metadata change |

Executed by `COMPOSE_PROJECT_NAME=dbdverify-l11-20261126 node tests/live.test.cjs`.
See [full raw output, IDs and restart commands](../../../../validation/README.md).
The run passed 14 workflow and 14 live checks. PostgreSQL outage separately returned
CatalogueReadFailed (exit 4); verification returned Matches after service recovery.

## Version IDs and row counts

## An additional check

## Storage decision
