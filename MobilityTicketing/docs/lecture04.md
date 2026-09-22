# Lecture 4: rolling product identity

The baseline uses `products.code` as the primary key and
`tickets.product_code` as a foreign key. Migration `030_expand_product_identity.sql`
adds a generated stable `products.product_id`, a nullable
`tickets.product_id`, an index, and a `NOT VALID` foreign key while preserving
the old columns. Old readers and writers continue using `product_code`; new
code can read and write through `product_id` during the compatibility window.

Migration `031_backfill_ticket_product.sql` maps existing codes to IDs only
where `tickets.product_id` is null and raises an error if any mapping remains
unresolved. Re-running it is therefore idempotent for already-correct rows.

Migration `032_contract_product_identity.sql` is intentionally a later
deployment. It validates the new foreign key, requires non-null IDs, removes
the old ticket foreign key and column, and changes the product primary key to
`product_id` without `CASCADE`. It must not be applied until old readers and
writers have been retired and catalog dependencies have been inspected.

The isolated experiment demonstrates the compatibility state, two backfill
passes, and the expected `NOT NULL` failure inside a transaction. It does not
apply the contract migration.

There is no EF Core project in this repository. An EF-style generated
migration would still need the same expand/backfill/validate/contract ordering;
the handwritten SQL remains the executable migration because PostgreSQL is the
only application technology present.

## Illustrative EF Core comparison

The following is an illustrative, non-executed EF Core-style migration. No EF
Core package, project, or generated migration was added to this repository.

```csharp
// Illustrative only: not compiled or executed.
protected override void Up(MigrationBuilder migrationBuilder)
{
	migrationBuilder.AddColumn<long>(
		name: "product_id",
		table: "tickets",
		nullable: true);

	migrationBuilder.CreateIndex(
		name: "IX_tickets_product_id",
		table: "tickets",
		column: "product_id");

	migrationBuilder.AddForeignKey(
		name: "tickets_product_id_fk",
		table: "tickets",
		column: "product_id",
		principalTable: "products",
		principalColumn: "product_id",
		onDelete: ReferentialAction.Restrict);
}
```

That generated-style operation represents only the expand phase. The
handwritten PostgreSQL sequence deliberately separates responsibilities:

| Concern | Handwritten SQL | Typical generated migration |
|---|---|---|
| Ordering | Expand, backfill, validate, then contract across deployments | Usually schema operations in one generated `Up` method |
| Nullable expand | `030` adds nullable `tickets.product_id` | Must be explicitly configured; making it required immediately breaks old rows |
| Old/new compatibility | Old `product_code` readers and writers remain valid during expand | Not normally inferred from the model snapshot |
| Backfill | `031` maps codes, exposes unresolved rows, and is idempotent | Data movement usually needs hand-written SQL or a separate deployment |
| Idempotency | `030` uses guarded additions; `031` updates only null mappings | Generated migrations are normally intended to run once |
| FK validation | `030` adds `NOT VALID`; `032` validates after backfill | A generated FK often validates immediately, making rollout order unsafe |
| Delayed `NOT NULL` | `032` applies it only after verification | A model change can generate a mandatory column too early |
| Dependency inspection | `032` is preceded by catalog inspection and avoids `CASCADE` | Tooling cannot know every old reader, writer, view, or deployment |
| Rollback | There is no automatic safe rollback after old code is retired or data is rewritten | Generated `Down` methods may be syntactically reversible but operationally unsafe |
| Operational safety | Each phase is reviewed, observed, and deployed separately | Generated SQL still needs a rollout plan and production data rehearsal |

An ORM migration generator normally sees the desired final model, not the
rolling-deployment contract between old and new application versions. It cannot
reliably infer when old writers have stopped sending `product_code`, whether a
backfill is complete, which dependencies are outside the ORM model, or when a
foreign key should be validated. The illustrative example therefore documents
the shape of an ORM-generated expand step, while `030`-`032` remain the
executable and operationally safe implementation.

## Migration and experiment execution order

Apply baseline schema and seed files first because the draft ticketing schema
is intentionally unconstrained. Apply integrity migrations `011`-`018`, then
reporting migration `020`, then product migrations `030` and `031`. Run the
post-contract negative checks only after `032`; run the historical
`constraints_should_fail.sql` before `032` because it intentionally references
the legacy `product_code` model. Expected constraint errors are test evidence;
any unexpected SQL error or missing object is a failed verification.

Migrations `017`, `018`, `020`, and `030` are transaction-bounded schema
deployments and should be treated as ordered deployments, not blindly rerun on
an already-contracted database. `031` is the explicitly idempotent backfill and
is safe to rerun. The lecture experiments use transactions and roll back their
temporary data, so they are repeatable. `032` is a one-way contract step and
must be applied once after dependency and compatibility checks.