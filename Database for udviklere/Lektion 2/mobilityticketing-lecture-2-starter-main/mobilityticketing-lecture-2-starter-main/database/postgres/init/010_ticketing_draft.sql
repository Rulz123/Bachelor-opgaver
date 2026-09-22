alter table trips
    add column capacity integer,
    add column reserved_seats integer default 0 not null check (reserved_seats >= 0 and reserved_seats <= capacity);
    

create table products (
    code text primary key,
    name text,
    price numeric not null check (price >= 0),
    currency text
);

create table users (
    id text primary key,
    email text,
    full_name text,
    is_disabled boolean default false
);

create table tickets (
    id text primary key,
    user_id text,
    trip_id text,
    ticket_code text,
    status text,
    product_code text,
    valid_from_utc timestamptz,
    valid_to_utc timestamptz,
    price numeric not null check (price >= 0),
    currency text    
);

create table payments (
    id text primary key,
    user_id text,
    ticket_id text,
    external_payment_reference text,
    amount numeric not null check (amount >= 0),
    currency text not null,
    status text,
    created_utc timestamptz default now()    
);

create table validations (
    id text primary key,
    ticket_id text not null,
    ticket_code text not null references tickets(ticket_code),
    vehicle_id text not null references ,
    stop_id text not null references stops(stop_id),
    device_id text,
    result text,
    validated_utc timestamptz default now()
);

-- The absence of constraints is intentional.