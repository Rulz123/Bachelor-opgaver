begin;

-- Supply missing capacities for our Lecture 1 test trips.
update trips
set capacity = 80
where route_id = 'LINE-5C' and capacity is null;

update trips
set capacity = 120
where route_id = 'LINE-M2' and capacity is null;

-- Require known capacities and valid reservation counts.
alter table trips
    alter column capacity set not null,
    alter column reserved_seats set not null,
    add constraint trips_capacity_non_negative
        check (capacity >= 0),
    add constraint trips_reserved_seats_valid
        check (reserved_seats between 0 and capacity);

commit;