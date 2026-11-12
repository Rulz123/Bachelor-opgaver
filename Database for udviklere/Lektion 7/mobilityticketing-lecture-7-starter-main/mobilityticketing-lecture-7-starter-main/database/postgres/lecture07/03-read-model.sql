-- Design persistent storage for published dispatch plans in lab07.
-- Include the plan identity, time window, publication time and displayed values.
-- The contract defines behavior; the table layout is your choice.

create table lab07.dispatch_plans{
    id text primary key,
    route_id text not null,
    departure timestampz not null,
    arrival timestampz not null,
    published_at timestampz not null,
    status text not null check (status in ('Scheduled', 'Cancelled')),
}