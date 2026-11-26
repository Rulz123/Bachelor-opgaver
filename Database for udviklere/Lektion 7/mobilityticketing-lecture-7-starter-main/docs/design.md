# Lecture 7 design

`StudentApplication` validates inputs and coordinates commands. `LiveRouteOverviewQuery` projects only DTO fields in one SQL read, including empty routes. No lazy-loading loop and no Route/Trip entities are needed for the board.

Commands use the supplied `DatabaseSession.WriteAsync`: one transaction per operation, rollback on failure and stable advisory keys. Bookings serialize by request ID, then lock the trip row; cancellation locks that same trip row. An existing receipt is resolved before current trip status, so retrying after cancellation returns the original receipt. A request reused for another trip conflicts. Times come from the supplied clock and are truncated to PostgreSQL microseconds before returning a receipt.

Publication stores one JSONB document containing all displayed values, the UTC window and publication time. Header columns make retry-window comparison explicit. Same ID/window preserves the first version; a different window conflicts. Publication invokes the supplied failure hook before commit. ReadSnapshot loads only the JSON payload in one read. Later renames and cancellations do not modify saved documents.

This is CQRS within one application and PostgreSQL database: separate reads and commands do not require microservices or separate databases. JSON snapshots trade SQL analytics flexibility for a simple immutable read model.

## Student tests

- Receipt retry after cancellation with a new application: persistence and original timestamp.
- Concurrent reuse across different trips: exactly one booking and one conflict.
- Failure before publication commit: no partial snapshot; retry succeeds.
- Empty publication window: rejection without writes.

The fixture intentionally drops and rebuilds lab07 before each test. A new DbContext does not create a new database.
