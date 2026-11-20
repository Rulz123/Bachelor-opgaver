Lecture 9 — MobilityTicketing
Evidence and experiment log

Database: PostgreSQL 17
Database name: mobility_l09
Schema: lab09
Function: lab09.book(text, text, text) RETURNS bigint
Isolation levels tested: READ COMMITTED, REPEATABLE READ, SERIALIZABLE

Task 1 — Reproduce the double booking
Experiment 1.1 — Unsafe read-then-write at READ COMMITTED

Isolation level: READ COMMITTED

Session A
Began a transaction.
Read T-LAST availability.
Observed reserved_seats = 0.
Determined that a seat was available.
Session B
Began a transaction.
Read T-LAST availability before A wrote.
Observed reserved_seats = 0.
Determined that a seat was available.
Write sequence
Session A updated the counter to 1.
Session A inserted booking unsafe-A.
Session A committed.
Session B updated the counter using its previously observed value of 0.
Session B inserted booking unsafe-B.
Session B committed.
Result

Two booking rows could exist while reserved_seats remained 1.

The problem was that B wrote back a stale value based on its earlier read.

BEGIN and COMMIT did not prevent the anomaly because READ COMMITTED does not protect a value read in an earlier statement from concurrent modification.

The CHECK constraint accepted the result because reserved_seats = 1 was within the permitted range of zero to capacity. The constraint does not compare the counter with the number of booking rows.

Experiment 1.2 — Increment without a capacity condition

The unsafe update was changed to:

UPDATE lab09.trips SET reserved_seats = reserved_seats + 1 WHERE id = 'T-LAST';

Both booking insertion and counter update were kept in the same transaction.

Expected result: The second increment attempts to exceed capacity and violates the CHECK constraint.

SQLSTATE: 23514 (check_violation).

The failed transaction must be rolled back. Its booking insertion and counter update are both undone.

The CHECK constraint protects the allowed counter range, but it does not prove that the number of bookings agrees with the counter.

If the booking insertion committed separately, a failed counter update could leave a booking without a corresponding reserved seat.

Task 2 — Conditional update
Implementation

The conditional update increments the counter only if:

The trip exists.
The trip is Scheduled.
Reserved seats are below capacity.

The UPDATE uses RETURNING to determine whether a seat was successfully reserved.

The booking insertion and counter update are performed in the same transaction.

A/B experiment — A commits

Isolation level: READ COMMITTED

A began a transaction and called lab09.book for T-LAST.
A left its transaction open.
B began a transaction and attempted a different booking for T-LAST.
B waited for A's row update.
A committed.
B resumed and found no available seat.
B's booking attempt was rejected.

Final expected result: One booking and one reserved seat.

A/B experiment — A rolls back
A booked T-LAST but left the transaction open.
B attempted another booking and waited.
A rolled back.
B resumed and successfully booked the seat.
B committed.

Final expected result: One booking and one reserved seat.

Explanation

The conditional UPDATE is the statement that makes the capacity decision.

Reading availability first is insufficient because another transaction can change the counter between the read and the write.

The conditional UPDATE can wait for another writer. After the competing transaction finishes, PostgreSQL evaluates the update condition against the current row.

A zero-row result indicates that no row met the update conditions. It does not distinguish between a missing trip, a cancelled trip or a full trip.

Task 3 — Explicit locks

Three approaches were implemented and compared.

3.1 Row lock

Implementation: SELECT ... FOR UPDATE.

The row lock was acquired before checking availability, changing the counter and inserting the booking.

Experiment
A acquired a row lock on T-LAST.
B attempted to acquire a lock on T-LAST.
B waited.
A released its lock.
B continued.

A separate experiment used T-OTHER.

Result: A lock on T-LAST did not prevent B from locking T-OTHER.

Explanation

Row locks allow transactions booking different trips to proceed concurrently.

However, a transaction holding a row lock prevents competing transactions from modifying or locking that same row until it finishes.

Long transactions therefore increase waiting time for passengers booking the same trip.

A commits and A rolls back experiments were performed.

Actual booking counts and SQLSTATEs: [Insert observed results.]

3.2 Table lock

Implementation:

LOCK TABLE lab09.trips IN EXCLUSIVE MODE;

The table lock was acquired before reading availability.

Experiment
A acquired the table lock.
B attempted to book T-OTHER.
B waited despite targeting a different trip.
A completed its transaction.
B continued.
Explanation

The table lock prevents competing transactions from obtaining conflicting table locks, including when they want different trips.

This reduces concurrency compared with row-level locking.

A commits and A rolls back experiments were performed.

Actual waiting and final results: [Insert observed results.]

3.3 Transaction advisory lock

Implementation: pg_advisory_xact_lock.

Namespace: 9009.

Trip	Lock key
T-LAST	1
T-TWO	2
T-CANCELLED	3
T-OTHER	4

The same two-integer mapping was used in cooperating booking paths.

Experiment

A acquired the advisory lock for T-LAST.

B attempted to acquire the same advisory lock and waited.

A released the transaction lock by ending its transaction.

B then continued.

An additional experiment deliberately ran the unsafe booking path in B without acquiring the advisory lock.

Explanation

Advisory locks are cooperative. PostgreSQL does not automatically enforce them on code that ignores the convention.

An unsafe path that does not acquire the advisory lock can bypass the advisory-lock protocol.

Updates still take their normal PostgreSQL row locks.

Actual waiting, errors and final results: [Insert observed results.]

3.4 Deadlock experiment

A acquired the row lock on T-LAST.

B acquired the row lock on T-OTHER.

A then requested T-OTHER.

B then requested T-LAST.

This created a circular wait.

Expected PostgreSQL error:

SQLSTATE 40P01 — deadlock_detected.

PostgreSQL aborts one transaction to resolve the deadlock.

Both sessions were rolled back.

Consistent lock ordering

The experiment was repeated with both sessions acquiring rows in the same order.

Using a consistent ordering avoids the circular-wait pattern demonstrated in the first experiment.

Explanation

After a deadlock, the application should roll back and retry the entire transaction, not simply repeat the failed statement.

Actual victim session and output: [Insert observed result.]

Task 4 — Optimistic concurrency check
Implementation

The optimistic version:

Reads the current reserved-seat count.
Stores the observed value.
Updates only if the counter still equals the observed value.
Checks capacity and trip status.
Uses RETURNING or FOUND to detect failure.
Inserts the booking in the same transaction.

The schema does not contain a version column.

Experiment 4.1 — T-TWO, A commits

Isolation level: READ COMMITTED

A read the reserved-seat count.
B read the same reserved-seat count.
Both observed the same value.
A performed its conditional update.
A inserted its booking and committed.
B attempted its update using the old observed count.
B's update affected zero rows.

B rolled back its failed attempt and started a new transaction.

B reread availability and retried.

Explanation

A failed optimistic check does not necessarily mean that the trip is full.

It means the value changed since the original read. Another passenger may have changed the counter while seats are still available.

Experiment 4.2 — T-LAST, A rolls back

A and B both read the same initial count.

A performed its update but subsequently rolled back.

B then attempted its optimistic update.

Actual result: [Insert observed result.]

The experiment demonstrates that optimistic checks do not eliminate locking. UPDATE statements still acquire row locks.

Version-column discussion

Comparing only reserved_seats can miss an ABA change: the counter could change and later return to its original value.

A version column incremented on every relevant modification would detect that history.

The update could check both the observed counter and the observed version.

Task 5 — Compare isolation levels
5.1 READ COMMITTED

The original unsafe read-then-write experiment was first checked.

Both sessions used READ COMMITTED and read availability before either wrote.

The experiment demonstrated that stale values could be written back without preventing both bookings from committing.

Result: The unsafe sequence allowed the double-booking anomaly.

5.2 REPEATABLE READ

An initial attempt was accidentally performed under READ COMMITTED.

The session isolation settings were corrected and verified using:

SELECT current_setting('transaction_isolation');

Both sessions were then explicitly started with:

BEGIN ISOLATION LEVEL REPEATABLE READ;

A/B sequence
A began a Repeatable Read transaction.
A read T-LAST and observed zero reserved seats.
B began a Repeatable Read transaction.
B read T-LAST and also observed zero reserved seats.
A performed the unsafe update and inserted its booking.
A committed.
B attempted its unsafe update.
B waited for A's transaction to finish.
B received a serialization failure.

Observed SQLSTATE: 40001.

B was rolled back.

Final result

T-LAST contained:

One booking.
One reserved seat.
Matching booking and reservation counts.
Retry

B started a completely new transaction and repeated the availability read.

The new read showed that T-LAST was full:

can_book = false.

The booking was not attempted again.

Explanation

Retrying only the failed UPDATE with the old observed value would be insufficient.

The application must restart the complete transaction, including its reads and business decisions.

5.3 SERIALIZABLE

Both sessions were explicitly started with:

BEGIN ISOLATION LEVEL SERIALIZABLE;

The setting was verified using:

SELECT current_setting('transaction_isolation');

Both sessions reported serializable.

A/B sequence
A began a Serializable transaction.
A read T-LAST and observed zero reserved seats.
B began a Serializable transaction.
B read T-LAST and observed zero reserved seats.
A performed the unsafe update and inserted its booking.
A committed.
B attempted its unsafe update.
B waited.
B received a serialization failure.

Observed SQLSTATE: 40001.

B was rolled back.

Final result

Inspection confirmed that T-LAST had one booking and one reserved seat.

Retry

B started a new Serializable transaction and repeated the availability read.

The result was:

can_book = false.

No second booking was inserted.

Comparison

Repeatable Read and Serializable produced similar results in this particular experiment because both transactions attempted to update the same row after reading an older version.

However, their guarantees differ.

PostgreSQL Repeatable Read prevents nonrepeatable reads and many anomalies but still permits serialization anomalies such as write skew.

Serializable provides stronger guarantees by detecting executions that cannot be equivalent to a serial execution.

Protecting one shared counter does not prove that every multi-row business rule is safe.

Application retry policy

For SQLSTATE 40001:

Roll back the failed transaction.
Start a new transaction.
Repeat all relevant reads.
Repeat all business decisions.
Repeat all database writes.
Limit retries to a defined number of attempts.
If retries continue failing, return a temporary failure to the passenger.

External payment operations should not be repeated blindly inside the retry loop.

A repeated payment could charge the passenger more than once. Payments should use an idempotency mechanism and be coordinated separately from database transaction retries.

Task 6 — Handle repeated requests
Implementation

The final lab09.book function includes:

An initial lookup by request_id.
Validation of the original passenger and trip.
A conditional capacity update.
An atomic booking insertion.
Handling of concurrent identical requests.
A unique request_id constraint.
Caller-controlled transaction management.

The function does not commit internally.

6.1 Sequential duplicate request

A booking was created on T-LAST.

The same request ID, passenger and trip were submitted again.

Observed result: The original booking ID was returned.

The trip was already full, but the existing booking was correctly returned.

6.2 Reuse request ID with a different passenger

The successful request ID was reused with passenger-B.

Observed result: Rejected.

The original booking remained unchanged.

6.3 Reuse request ID with a different trip

The successful request ID was reused with T-OTHER.

Observed result: Rejected.

Neither trip's counter was changed by the rejected attempt.

6.4 Concurrent identical requests — A commits

Isolation level: READ COMMITTED.

Session A

A began a transaction and called:

SELECT lab09.book( 'repeat-concurrent', 'passenger-A', 'T-TWO' );

A received a booking ID and left the transaction open.

Session B

B began a transaction and called the same function with the same request ID and details.

B waited for A.

A committed.

B resumed and returned the original booking ID.

B committed.

Final result

T-TWO contained:

One booking.
One reserved seat.
Matching counts.
6.5 Concurrent identical requests — A rolls back

The fixture was reset.

A called the function and left its transaction open.

B called the same function and waited.

A rolled back.

B resumed and successfully created the booking.

B committed.

Final result

T-TWO contained:

One booking.
One reserved seat.
Matching counts.
Explanation

The unique request ID prevents duplicate records for the same request.

It does not prevent two different request IDs from competing for the last seat. Capacity protection is provided by the conditional UPDATE.

Checking for an existing request ID before insertion is not sufficient by itself because two transactions can both observe that the request ID does not exist.

The database unique constraint is essential.

A returned booking ID is not confirmed as committed until the caller's transaction commits.

6.6 Supplied checks

Command:

docker compose exec -T postgres bash /lab/run.sh check

Observed output: [Paste actual check output here.]

The supplied checks cover:

Successful booking.
Repeated request returning the original ID.
Repeated request after cancellation.
Full trip rejection.
Different passenger rejection.
Different trip rejection.
Cancelled trip rejection.
Missing trip rejection.
Distinct successful requests.
Rollback after a successful function call.
Agreement between booking counts and reserved-seat counters.
Task 7 — AI review
AI assistant

Model: GPT-6 (OpenAI)

Review prompt

The AI was asked to review:

The database schema.
The final lab09.book implementation.
The two-session concurrency experiments.

The review was asked to identify a potential concurrency or retry weakness and propose an exact A/B sequence to expose it.

AI finding

The AI identified a potential issue with concurrent identical requests under REPEATABLE READ and SERIALIZABLE.

The function handles unique_violation, but it does not internally handle serialization failures.

At stronger isolation levels, a transaction may retain an older snapshot and receive SQLSTATE 40001 when a concurrent transaction updates the same trip row.

The application must roll back and retry the complete transaction.

Proposed experiment

Two sessions use:

BEGIN ISOLATION LEVEL REPEATABLE READ;

Both call:

SELECT lab09.book( 'ai-review-001', 'passenger-A', 'T-TWO' );

A leaves its transaction open.

B attempts the same request and waits.

A commits.

B is expected to receive SQLSTATE 40001 when it attempts to update the concurrently changed trip row.

B rolls back and retries the entire transaction.

The retry should find the original booking and return its ID.

Prediction
A creates the booking.
B waits.
A commits.
B receives 40001.
B rolls back.
B retries the complete transaction.
B returns A's original booking ID.
Final state: one booking and one reserved seat.
Actual experiment

Status: [Complete after running the proposed A/B experiment.]

A's booking ID: [Insert result.]

B's first-attempt error and SQLSTATE: [Insert result.]

B's retry booking ID: [Insert result.]

Final booking count: [Insert result.]

Final reserved-seat count: [Insert result.]

Acceptance or rejection

[Explain whether the experiment supported the AI's concern.]

The distinction is that a serialization failure does not automatically mean the booking function is incorrect. It means the calling application must handle transaction retries appropriately.

Conclusion

The AI review identified that idempotency handling and serialization retry handling are separate concerns.

The unique request constraint protects against duplicate booking records. The conditional update protects capacity. Transaction retry handling remains the application's responsibility when PostgreSQL reports SQLSTATE 40001.

The completed experiments provide evidence for the tested concurrency scenarios. They do not prove that every possible concurrent workload or multi-row business rule is safe.

Overall conclusion

This lab demonstrated several ways to protect a shared booking counter in PostgreSQL.

The unsafe READ COMMITTED approach allowed stale reads and double booking.

Conditional updates provided a direct capacity check while allowing PostgreSQL to coordinate competing writers.

Explicit row locks provided trip-level coordination, while table locks reduced concurrency and advisory locks depended on cooperating application code.

Optimistic checks detected changes between reading and updating, although they did not eliminate row locking.

REPEATABLE READ and SERIALIZABLE rejected conflicting stale updates with serialization failures. Applications must retry the complete transaction after SQLSTATE 40001.

The final booking function combines conditional capacity protection with request-ID idempotency. Concurrent identical requests were tested under READ COMMITTED, including both commit and rollback scenarios.

The AI review highlighted the importance of distinguishing database correctness from application retry behaviour.

Final assessment: The implementation meets the tested booking requirements, subject to completing the outstanding evidence entries and confirming the supplied checks and Task 7 experiment.