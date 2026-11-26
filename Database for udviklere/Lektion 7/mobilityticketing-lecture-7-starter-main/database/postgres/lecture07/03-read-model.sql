CREATE TABLE lab07.published_snapshots (
    id text PRIMARY KEY,
    from_utc timestamptz NOT NULL,
    to_utc timestamptz NOT NULL,
    payload jsonb NOT NULL,
    CHECK (to_utc > from_utc AND to_utc - from_utc <= interval '24 hours')
);
