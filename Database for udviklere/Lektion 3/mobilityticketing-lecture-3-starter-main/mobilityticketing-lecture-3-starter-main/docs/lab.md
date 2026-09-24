# Implementation lab: Where should reporting logic execute?

## Purpose

Implement and compare a direct SQL query, a user-defined function, a materialized view, and a trigger-maintained summary for daily captured revenue. Use the differences to decide where the responsibility belongs.

The aim is not to simply choose the option with the most or the least SQL. Make a fair comparison of the options. Take into account execution timing, dependencies, transaction scope, freshness, and recovery.

## Scenario

Operators need daily captured revenue. The initial design proposes a `daily_revenue_by_operator` table maintained when payments are inserted. Reports may also be calculated directly from the transactional tables or exposed through a materialized view.

The `payments` table is the authority for this experiment. Treat the stored reporting results as derived data.

## Before you start

Start the database from the repository root:

```bash
docker compose up -d
docker compose ps
```

Run the base query in [`../database/postgres/queries/base_revenue.sql`](../database/postgres/queries/base_revenue.sql) and save its result. Do not edit the files in `database/postgres/init/`.

## Tasks

1. Run the reference revenue query and verify its result from the base tables.

the sql calculation is the most consistent, but also the heaviest, as it relies on alot of scripts to look into different tables for different rows.

2. Wrap the read logic in a SQL function.

The function makes it easier to use all around, but has a few problems with duplicates in the display. It requires alot more work than the others, but seems to be easy to scale. Likely takes a lot more maintenance.

3. Create a materialized view and observe when it becomes stale.

Materialized view is not the best if you want responsiveness, but it is easy to reuse. The downside is the constant refreshing when it has to be used. There is a high likelyhood of getting old data that is not correct anymore. Should be used with a trigger to refresh on changes imo.

4. Create the supplied trigger-maintained summary.

The trigger is good for updating the field per operator, and makes it so it is live data, but it doesn't show collective data, only for that one operator.
The trigger does only listen for captured entries, so it might retain old data from refunds.
Very good for full automation, but requires a bit of maintenance. Works well with functions to add more complex work.

5. Test all four approaches against:
   - a captured payment insert;
   - a failed payment insert;
   - a status correction from `Failed` to `Captured`;
   - a correction from `Captured` to `Refunded`;
   - deletion or replacement of test data;
   - duplicate delivery of the same external payment reference.
6. Produce a responsibility matrix comparing correctness, freshness, write cost, read cost, hidden side effects, rebuildability, and operational complexity.
7. Recommend one approach for the current case. A hybrid answer is allowed, but each stored copy must have a clear authority and rebuild path.

Depending on the current servers performance, i would choose a few different things. 

If there is alot of performance, i would likely go with the trigger and functions way. It is full automation, but has some maintenance when changes will happen. Can easily be worked around, and can be stored as old procedures for back rolling.

If the performance is not the best, i would go with the materialized view with a refresh trigger. 

The migration examples identify the intended object names. Complete them in dependency order and apply them from the repository root:

```bash
docker compose exec -T postgres psql -U mobility -d mobility < database/postgres/migrations/020_reporting_function.sql
docker compose exec -T postgres psql -U mobility -d mobility < database/postgres/migrations/021_daily_revenue_trigger.sql
docker compose exec -T postgres psql -U mobility -d mobility < database/postgres/migrations/022_daily_captured_revenue.sql
```

Use a clean container when you need to repeat the experiment:

```bash
docker compose down
docker compose up -d
```

## Required evidence

- SQL object definitions for all four approaches.
- Output before and after each test case.
- One captured example where two approaches disagree.
- A side-effect trace showing everything caused by one payment write.
- A responsibility matrix with a named authority, freshness rule, and rebuild path.
- One issue in the issue register and one defended decision record.

## Side-effect trace

For one `INSERT INTO payments`, record:

Side effects can be traced in the picture folder. There is pictures for the different steps, numbered after the case number.

1. constraints or references checked;
2. trigger execution, if any;
3. summary-table writes;
4. rows or locks touched, where observable;
5. commit or rollback behaviour;
6. the point at which each report becomes current;
7. what the application can observe.

## Your recommendation

What's your final recommendation?

It's important to not **only** choose the fastest option. Think about the effects on coupling, correction behavior, duplicate delivery, observability and recovery.

Remember that the lab does not require a final architecture. We will address problems like ticket-purchase concurrency and payment capture in later lectures.
