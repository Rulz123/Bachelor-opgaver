# Lecture 4 — Evidence index

## Scope and current state

This index links the existing evidence; all learning notes, the compatibility matrix and the rollout decision remain in [docs/notes.md](../notes.md#lecture-4--product-identity-migration).

[Starter lab](lab4.md) · [AI-drafted EF Core comparison](ef-core-comparison.md)

Work stayed on `main` at the student's request. Migrations 030–032 were applied. The ID-only removal rehearsal was rolled back: `tickets.product_id` is required and `tickets.product_code` still exists. Six tickets remain in the last recorded verification. No permanent legacy-column removal is claimed.

## SQL and recorded outcomes

| Task | SQL | Recorded result | Finding |
|---|---|---|---|
| Baseline | [baseline.sql](../../database/postgres/experiments/lecture04/baseline.sql) | [Output](lecture4-baseline.txt) | Three original tickets, two products; DAY price 65 DKK |
| Unsafe removal | [unsafe_change.sql](../../database/postgres/experiments/lecture04/unsafe_change.sql) | [Output](lecture4-unsafe-change.txt) | 42703; rollback restored data |
| Expansion | [030_expand_product_identity.sql](../../database/postgres/migrations/lecture04/030_expand_product_identity.sql) | [Output](lecture4-expanded.txt) | Two stored product UUIDs; ticket references initially null |
| Old/new readers | [old_writer.sql](../../database/postgres/experiments/lecture04/old_writer.sql), [old_reader.sql](../../database/postgres/experiments/lecture04/old_reader.sql), [new_reader.sql](../../database/postgres/experiments/lecture04/new_reader.sql) | [Output](lecture4-reader-compatibility.txt) | Old insert works; fallback reader resolves four tickets |
| New writer | [new_writer.sql](../../database/postgres/experiments/lecture04/new_writer.sql) | [Output](lecture4-new-writer.txt) | DAY ID/code, agreed price 65 |
| Direct mismatch | [mismatched_pair.sql](../../database/postgres/experiments/lecture04/mismatched_pair.sql) | [Output](lecture4-mismatched-pair.txt) | Independent FKs permit mismatch; rolled back |
| Repeat backfill | [backfill_test.sql](../../database/postgres/experiments/lecture04/backfill_test.sql), [031_backfill_ticket_product.sql](../../database/postgres/migrations/lecture04/031_backfill_ticket_product.sql) | [Output](lecture4-backfill.txt) | UPDATE 4, then UPDATE 0 |
| Late old writer | [late_write_test.sql](../../database/postgres/experiments/lecture04/late_write_test.sql), [late_old_writer.sql](../../database/postgres/experiments/lecture04/late_old_writer.sql) | [Output](lecture4-late-write.txt) | Missing ID filled; UPDATE 1, then UPDATE 0 |
| Verification | [verification_test.sql](../../database/postgres/experiments/lecture04/verification_test.sql), [verify.sql](../../database/postgres/experiments/lecture04/verify.sql) | [Output](lecture4-verification.txt) | Detects deliberate mismatch; original values preserved |
| Early NOT NULL failure | [require_id_failure.sql](../../database/postgres/experiments/lecture04/require_id_failure.sql) | [Output](lecture4-require-id-failure.txt) | 23502; rolled back |
| Require ID | [032_require_ticket_product.sql](../../database/postgres/migrations/lecture04/032_require_ticket_product.sql) | [Output](lecture4-require-id-success.txt) | Validated FK and required ID committed |
| Old writer rejected | [old_writer_rejected.sql](../../database/postgres/experiments/lecture04/old_writer_rejected.sql) | [Output](lecture4-old-writer-rejected.txt) | 23502; old reader still returns six tickets |
| Removal rehearsal | [remove_legacy.sql](../../database/postgres/experiments/lecture04/remove_legacy.sql), [final_reader.sql](../../database/postgres/experiments/lecture04/final_reader.sql), [final_writer.sql](../../database/postgres/experiments/lecture04/final_writer.sql) | [Output](lecture4-remove-legacy.txt) | No matching reporting definitions; seven tickets inside transaction, six after rollback |
| Unknown product | [new_writer.sql](../../database/postgres/experiments/lecture04/new_writer.sql) | [Output](lecture4-unknown-product.txt) | 23502 from missing derived code; no insert |
| Conflicting caller code | [conflicting_input.sql](../../database/postgres/experiments/lecture04/conflicting_input.sql) | [Output](lecture4-conflicting-input.txt) | Unused SINGLE input ignored; writer derives DAY; rolled back |

## Commands and execution order

Commands below document the run, not a request to replay the experiment against its current state. DDL and fixed-ID inserts are not generally rerunnable. The baseline fixture is [013_migration_fixture.sql](../../database/postgres/experiments/lecture04/013_migration_fixture.sql). It was applied once before recording the baseline. Earlier lecture constraints and reporting objects were retained.

From the repository root in PowerShell, a normal evidence capture uses:

```powershell
docker compose exec -T postgres psql -U mobility -d mobility -v ON_ERROR_STOP=1 -P pager=off -f /docker-entrypoint-initdb.d/experiments/lecture04/baseline.sql | Out-File -Encoding utf8 docs/lecture04/lecture4-baseline.txt
```

For other scripts, replace the script and output names with the corresponding row above. Lecture 4 migration files use `/docker-entrypoint-initdb.d/migrations/lecture04/`. The expansion inspection used:

```powershell
docker compose exec -T postgres psql -U mobility -d mobility -v ON_ERROR_STOP=1 -P pager=off -c "select code, id from products order by code; select id, product_code, product_id, price, currency from tickets order by id;" | Out-File -Encoding utf8 docs/lecture04/lecture4-expanded.txt
```

The reader-compatibility run applied `old_writer.sql`, `old_reader.sql`, and `new_reader.sql` in that order using three `-f` arguments.

Scripts demonstrating expected errors temporarily disable `ON_ERROR_STOP`, record SQLSTATE, and roll back where needed. Capture their stderr too:

```powershell
docker compose exec -T postgres psql -U mobility -d mobility -P pager=off -f /docker-entrypoint-initdb.d/experiments/lecture04/unsafe_change.sql 2>&1 | Out-File -Encoding utf8 docs/lecture04/lecture4-unsafe-change.txt
```

The new writer used the following inputs. This UUID was observed in this run; freshly initialized databases generate different UUIDs.

```powershell
docker compose exec -T postgres psql -U mobility -d mobility -P pager=off -v ticket_id=LAB04-NEW-1 -v ticket_code=LAB04-CODE-NEW-1 -v product_id=0afd95e7-1dde-449c-9bd5-e7e2e4fa978d -v agreed_price=65 -v agreed_currency=DKK -f /docker-entrypoint-initdb.d/experiments/lecture04/new_writer.sql | Out-File -Encoding utf8 docs/lecture04/lecture4-new-writer.txt
```

The unknown-ID test used `LAB04-UNKNOWN`, `LAB04-CODE-UNKNOWN`, product UUID `00000000-0000-0000-0000-000000000000`, price 65 and currency DKK. It added `-v VERBOSITY=verbose` and `2>&1` to capture SQLSTATE. It failed without storing a ticket.

Execution order was: baseline → unsafe rehearsal → expansion → old/new readers → new writer → direct mismatch → repeat backfill → late old write/backfill → verification → early requirement failure → require ID → old-writer rejection → removal rehearsal → unknown-product test → conflicting-input test.

## Interpretation boundaries

- Unknown-ID rejection occurs because the derived code is null; the application could present a clearer message.
- The conflicting-code variable is an extra caller input deliberately unused by our writer. It demonstrates that the writer derives the code; it is not a database-level code/ID matching constraint.
- The removal rehearsal searches public definitions and avoids CASCADE. It is not proof that every external application has been updated.
- Earlier migration and historical experiment files intentionally still mention the old column.
- The compatibility matrix includes conclusions from SQL inspection as well as the executed cases linked above; no separate execution is claimed for every cell.
- An AI-authored EF comparison is documentation only, not a newly executed migration or installed ORM.

