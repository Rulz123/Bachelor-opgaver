## Prompt

I am completing a PostgreSQL concurrency lab involving a ticket-booking system.

Please review my database schema, final lab09.book() function and the following two-session experiment output.

Look for a concurrency problem, incorrect retry behaviour, or an assumption that my experiments have not adequately tested.

Please:

    Identify one specific potential weakness.
    Propose an exact sequence of SQL commands for sessions A and B that could expose it.
    Predict the expected database behaviour.
    Explain what evidence would confirm or reject your concern.

Do not assume that agreement with my implementation proves it is correct.

Here are my schema, function and experiment results:

[Paste schema, final book.sql and relevant A/B output here.]

## AI review — Potential weakness

Issue: Duplicate requests under REPEATABLE READ and SERIALIZABLE isolation.

Your function handles concurrent duplicate requests using:

    An initial lookup by request_id.

    A unique constraint on lab09.bookings.request_id.

    An exception handler for unique_violation.

This works in your READ COMMITTED experiment, but it does not necessarily behave the same way under stronger isolation levels.

At REPEATABLE READ or SERIALIZABLE, a transaction may retain an older snapshot. If another transaction inserts the same request ID and commits, the second transaction may not be able to see that booking through its original snapshot.

PostgreSQL may instead raise a serialization error (40001) or a uniqueness error (23505), depending on the execution path.

This means your application must be prepared to retry the entire transaction, not just the failed SQL statement.
Proposed experiment

Use T-TWO and the same request ID in both sessions.

First reset:
bash

docker compose exec -T postgres bash /lab/run.sh reset

Session A
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

Leave A open.
Session B
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

B should wait.
Session A
sql

COMMIT;

Session B

Observe the result.

If B receives an error, record its SQLSTATE and then:
sql

ROLLBACK;

Start a completely new transaction:
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

COMMIT;

Predicted results

Stage
	

Expected behaviour

A calls function
	

Creates booking

B calls function
	

Waits

A commits
	

Booking becomes visible

B resumes
	

May receive 40001

B retries whole transaction
	

Returns A's original booking ID

The exact error from B is something we must establish experimentally.
Evidence to collect

Record:

    Isolation level in both sessions.

    Booking ID returned by A.

    Whether B waits.

    B's error and SQLSTATE, if any.

    Whether B's retry returns the original ID.

    Final booking count and reserved-seat count.

AI review conclusion

My initial recommendation is to retain your current implementation unless the experiment demonstrates a problem.

If B receives a serialization failure, that does not automatically mean the function is incorrect. It means the application must retry the complete transaction.

The important distinction is that a successful function return is not the same as a committed booking.

My assessment is that your implementation has a sound concurrency strategy for READ COMMITTED, but there is an important isolation-level behaviour worth testing for Task 7.
1. AI review findings
Core concurrency design looks sound

Your function has several good protections:

Feature
	

Assessment

Conditional seat update
	

Correct

Booking and counter update in one transaction
	

Correct

Duplicate request lookup
	

Correct

Unique request ID constraint
	

Correct

Different passenger rejection
	

Correct

Different trip rejection
	

Correct

Concurrent duplicate handling at READ COMMITTED
	

Correct strategy

Caller-controlled commit/rollback
	

Correct
The potential weakness

Your function handles concurrent identical requests using a unique-violation exception handler.

At READ COMMITTED, this works because PostgreSQL obtains a fresh snapshot for subsequent statements. After B waits for A to commit, B can find A's booking in the exception handler.

However, at REPEATABLE READ, the behaviour is different.

If B established its transaction snapshot before A committed, B cannot simply refresh that snapshot. When B attempts to update the same trip row that A has changed, PostgreSQL can raise:
text

ERROR: could not serialize access due to concurrent update
SQLSTATE: 40001

Your exception handler catches unique_violation, not 40001.

That is not necessarily a defect in your function. It means the application must retry the entire transaction.
2. Proposed Task 7 experiment

We'll deliberately test the same request concurrently under REPEATABLE READ.
Step 1 — Reset the fixture

Make sure both sessions have ended their transactions.
sql

ROLLBACK;

Then, in a normal terminal:
bash

docker compose exec -T postgres bash /lab/run.sh reset

Step 2 — Session A

Run:
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

Record the returned booking ID.

Leave A open.
Step 3 — Session B

Run:
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

B should wait because A has updated the same trip row but has not committed.
Step 4 — Session A commits

Switch to A:
sql

COMMIT;

Step 5 — Observe session B

B should resume and is expected to receive:
text

ERROR: could not serialize access due to concurrent update
SQLSTATE: 40001

This happens during the conditional trip update, before B reaches the unique-violation handler.

In B, run:
sql

ROLLBACK;

Step 6 — Retry the entire transaction

In B:
sql

BEGIN ISOLATION LEVEL REPEATABLE READ;

SELECT lab09.book(
    'ai-review-001',
    'passenger-A',
    'T-TWO'
);

This time, the initial lookup should find A's committed booking.

Expected: B returns the original booking ID.

Then:
sql

COMMIT;

Step 7 — Inspect
sql

\i /lab/sql/inspect.sql

Expected:

T-TWO
	

Result

Reserved seats
	

1

Booking count
	

1

Counts agree
	

true
3. Predicted outcome

My prediction is:

    A successfully creates the booking.

    B waits for A.

    A commits.

    B receives 40001.

    B rolls back.

    B retries the entire transaction.

    B receives A's original booking ID.

    The database contains one booking and one reserved seat.

This would demonstrate that your function works correctly when the application handles serialization failures appropriately.