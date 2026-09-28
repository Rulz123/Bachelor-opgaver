# Migrations by lecture

| Folder | Topic | SQL sequence |
|---|---|---|
| [lecture02](lecture02/) | SQL constraints and integrity | 011–016 |
| [lecture03](lecture03/) | Reporting function, trigger and materialized view | 020–022 |
| [lecture04](lecture04/) | Product identity: expand, backfill, require | 030–032 |

Each folder keeps its supplied .sql.example templates beside the completed .sql files. Lecture 1's initial schema and seed remain directly in database/postgres because they are initialization files.

## Running a migration

From the repository root in PowerShell, use the lecture folder in the mounted path. For example:

~~~~powershell
docker compose exec -T postgres psql -U mobility -d mobility -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/migrations/lecture04/030_expand_product_identity.sql
~~~~

This is a path example, not an instruction to rerun an applied migration. Moving files does not change the database. Apply DDL migrations once, in their intended lecture stage; the 031 data backfill is deliberately repeatable. Review the lab instructions before running scripts.