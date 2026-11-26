# Databases for Developers — review guide

Branch: `work/josephine-dbd-complete`, based on Nichlas commit `d743bb3`.
See [results, isolated setup and exact rerun commands](validation/README.md), including
all 51 lecture 7 tests passing and an archive of the complete raw evidence.
Use only the documented `dbdverify-*` environments; never delete unrelated volumes.
This is local verification, not a Moodle submission.

| Lecture | Setup/code entry point |
|---|---|
| 1 | [Project](<Lektion 1/mobilityticketing-lecture-1-starter-main/mobilityticketing-lecture-1-starter-main/README.md>) |
| 2 | [Project](<Lektion 2/mobilityticketing-lecture-2-starter-main/mobilityticketing-lecture-2-starter-main/README.md>) |
| 3 | [Project](<Lektion 3/mobilityticketing-lecture-3-starter-main/mobilityticketing-lecture-3-starter-main/README.md>) |
| 4 | [Project](<Lektion 4/mobilityticketing-lecture-4-starter-main/mobilityticketing-lecture-4-starter-main/README.md>) |
| 5 | [Project](<Lektion 5/mobilityticketing-lecture-5-starter-main/mobilityticketing-lecture-5-starter-main/README.md>) |
| 6 | [Project](<Lektion 6/mobilityticketing-lecture-6-starter-main/mobilityticketing-lecture-6-starter-main/README.md>) |
| 7 | [Project](<Lektion 7/mobilityticketing-lecture-7-starter-main/README.md>) |
| 8 | [Project](<Lektion 8/mobilityticketing-lecture-8-starter-main/README.md>) |
| 9 | [Project](<Lektion 9/mobilityticketing-lecture-9-starter-main/mobilityticketing-lecture-9-starter-main/README.md>) |
| 10 | [Project](<Lektion 10/mobilityticketing-lecture-10-starter-main/mobilityticketing-lecture-10-starter-main/README.md>) |
| 11 | [Project](<Lektion 11/mobilityticketing-lecture-11-starter-main/mobilityticketing-lecture-11-starter-main/README.md>) |

## Two decisions to discuss

1. Lecture 7: project the live board to DTOs and persist immutable JSON snapshots separately.
	Measured large-fixture reads: 201 for lazy loading versus 1 for projection; old snapshots
	remain unchanged after source edits.
2. Lecture 9: update capacity and insert a booking in one transaction, using a unique request
	ID for retries. This prevents overselling under the tested concurrency, but does not make
	external payment atomic with PostgreSQL.

Remaining unverified experiments and operational limitations are listed in the validation report.
