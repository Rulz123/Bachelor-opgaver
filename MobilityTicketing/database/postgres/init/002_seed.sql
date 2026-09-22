insert into operators (id, name) values
    ('OP-METRO', 'City Metro'),
    ('OP-BUS', 'City Bus')
on conflict do nothing;

insert into routes (id, operator_id, city_id, mode, short_name) values
    ('LINE-M2', 'OP-METRO', 'CPH', 'metro', 'M2'),
    ('LINE-5C', 'OP-BUS', 'CPH', 'bus', '5C')
on conflict do nothing;

insert into stops (id, city_id, name) values
    ('STOP-NORREPORT', 'CPH', 'Nørreport'),
    ('STOP-KONGENS-NYTORV', 'CPH', 'Kongens Nytorv'),
    ('STOP-AIRPORT', 'CPH', 'Copenhagen Airport'),
    ('STOP-CENTRAL', 'CPH', 'Copenhagen Central Station')
on conflict do nothing;

insert into route_stops (route_id, stop_id, stop_sequence) values
    ('LINE-M2', 'STOP-NORREPORT', 1),
    ('LINE-M2', 'STOP-KONGENS-NYTORV', 2),
    ('LINE-M2', 'STOP-AIRPORT', 3),
    ('LINE-5C', 'STOP-NORREPORT', 1),
    ('LINE-5C', 'STOP-CENTRAL', 2)
on conflict do nothing;

insert into trips (
    id,
    route_id,
    service_date,
    scheduled_departure_utc,
    status
) values
    ('TRIP-M2-001', 'LINE-M2', '2026-09-01', '2026-09-01 08:00:00+00', 'scheduled'),
    ('TRIP-M2-002', 'LINE-M2', '2026-09-01', '2026-09-01 09:00:00+00', 'scheduled'),
    ('TRIP-5C-001', 'LINE-5C', '2026-09-01', '2026-09-01 08:15:00+00', 'scheduled'),
    ('TRIP-5C-002', 'LINE-5C', '2026-09-01', '2026-09-01 09:15:00+00', 'scheduled')
on conflict do nothing;
