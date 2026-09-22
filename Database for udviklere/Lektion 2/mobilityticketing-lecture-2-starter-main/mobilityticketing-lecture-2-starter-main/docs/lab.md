# Lecture 2 implementation lab: Make invalid states difficult to store

## Purpose

Strengthen the initial PostgreSQL schema for ticket purchase and validation. The focus is not API validation. The focus is what the database can guarantee when writes arrive from different applications, scripts, or future services.

## Starting point

The course starter infrastructure contains a deliberately weak schema for trips, products, tickets, payments, and validations. It accepts several states that conflict with the MobilityTicketing case.

Read the supplied DDL before changing anything. Add constraints through a new migration. Do not edit the starter DDL.

## Tasks

1. Extract at least ten domain invariants from the scenario and schema.
   1: Trips - reserved seats direct enforce constraint
   2: Products - price direct enforce constraint
   3: Tickets - price direct enforce constraint
   4: Payments - amount direct enforce constraint
   5: Tickets - user_id unique
   6: Tickets - trip_id unique
   7: Tickets - ticket_code unique
   8: Tickets - product_code unique
   9: Payments - user_id unique 
   10: Payments - ticket_id unique 

2. Classify each invariant as one of:
   - directly enforceable with a column or table constraint;
   - enforceable with a unique or exclusion rule;
   - dependent on more than one row or an external system;
   - currently ambiguous and requiring a domain decision.
3. Implement the constraints that belong in this lecture.
4. Write negative tests that attempt invalid inserts or updates.
5. Record at least two important invariants that cannot be solved by a simple constraint. Keep them for the transactions lecture.

Tickets vehicle id is not implemented yet, as it needs a table.
Time frame implementation also needs to be validated. 

## Minimum invariants to consider

- Capacity cannot be negative.
- Reserved seats cannot be negative or greater than capacity.
- Ticket price and payment amount cannot be negative.
- Currency must be present and consistently represented.
- Ticket codes must support unambiguous lookup.
- A payment must refer to an existing ticket.
- A validation must refer to an existing ticket.
- A ticket validity end cannot be earlier than its start.
- Status values must come from a known set.
- An external payment reference should not be recorded twice when it represents one captured payment.
- A validation must not combine the identifier of one ticket with the code of another.

## Required evidence

For every implemented invariant, include:

- the rule in plain language;
- the DDL used to enforce it;
- one write that succeeds;
- one write that fails;
- the database error or result that demonstrates enforcement.

Assert PostgreSQL SQLSTATE codes in automated tests where possible. Matching an English error string is brittle and language-dependent.

## State-transition trace

Trace ticket purchase and ticket validation at the row and relationship level. Describe which rows are inserted or updated and which references must already exist. Do not analyse concurrency yet.

Ticket validation is done via created timestamp in payments, that is using user_id and ticket_id to couple the customer and the ticket together. These references have to be made. 
There is also external_payment_reference, for if the customer has paid outside the mobilityticket app. 

## Delete and update behaviour

Inspect the relationships for historical tickets, payments, and validations. For each relationship, state whether deletion should be restricted, cascaded, soft-deleted, or governed by a retention policy. Also state whether an update should be allowed or rejected when it would change historical meaning.

Data retention should be done, but within reason. Depending on the useage of the server, there should be a good historic backlog of data, that can be used for improvements in other areas (For example via buisness intelligence).

An update should not be allowed when it changes historical meaning, as it would dillude our historic data.

## Integrity map

Create an integrity map that connects each business rule to its current owner, affected tables, expected failure behaviour, and remaining limitation.

## Important boundary

A row-level check can protect `reserved_seats <= capacity` for one row. It cannot arbitrate two concurrent purchases that both observe the same remaining capacity. Do not claim that this migration solves that race.

Payment capture across an external gateway and PostgreSQL also needs workflow design. A disabled-user purchase rule may be a domain or transaction policy depending on the case decision. Classify these limits explicitly.

I would use the API that is working with this database to run the validation logic of payment, so it inserts into the database if the purchase was valid or not. 

It can also be done with constraints, but it will make the database alot less scalable for future migrations, without making major upkeep.