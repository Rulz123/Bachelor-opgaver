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

insert into route_stops (name, route_id, stop_id, stop_sequence) values
    ('STOPROAD 1', 'LINE-M2', 'STOP-NORREPORT', 1),
    ('TRANSITROAD 2', 'LINE-M2', 'STOP-CENTRAL', 1),
    ('POORROAD 556', 'LINE-M2', 'STOP-NORREPORT', 2),
    ('STOPROAD 1', 'LINE-M2', 'STOP-CENTRAL', 2),
    ('SEEDROAD 10', 'LINE-M2', 'STOP-NORREPORT', 3),
    ('HURROAD 2', 'LINE-M2', 'STOP-CENTRAL', 3),
    ('EXITROAD 34', 'LINE-5C', 'STOP-AIRPORT',1),
    ('RICHROAD 2', 'LINE-5C', 'STOP-KONGENS-NYTORV',1),
    ('SEEDROAD 10', 'LINE-5C', 'STOP-AIRPORT', 2),
    ('KINGSROAD 2', 'LINE-5C', 'STOP-KONGENS-NYTORV', 2),
    ('LASTSTOP 10', 'LINE-5C', 'STOP-AIRPORT', 3),
    ('DURRROAD 1', 'LINE-5C', 'STOP-KONGENS-NYTORV', 3)
on conflict do nothing;


INSERT INTO trips (
    id,
    route_id,
    service_date,
    scheduled_departure_utc,
    status
) VALUES
    -- Yesterday
    ('trip_001', 'LINE-M2',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 6 hours',
        1),
    ('trip_002', 'LINE-M2',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 9 hours 30 minutes',
        1),
    ('trip_003', 'LINE-M2',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 14 hours',
        1),

    ('trip_004', 'LINE-5C',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 5 hours 15 minutes',
        1),
    ('trip_005', 'LINE-5C',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 8 hours 45 minutes',
        1),
    ('trip_006', 'LINE-5C',
        CURRENT_DATE - 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 day 16 hours',
        0),

    -- Today
    ('trip_007', 'LINE-M2',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '2 hours',
        1),
    ('trip_008', 'LINE-M2',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 hour 30 minutes',
        1),
    ('trip_009', 'LINE-M2',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '4 hours 15 minutes',
        1),
    ('trip_010', 'LINE-M2',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '7 hours',
        0),

    ('trip_011', 'LINE-5C',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') - INTERVAL '1 hour',
        1),
    ('trip_012', 'LINE-5C',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 hours',
        1),
    ('trip_013', 'LINE-5C',
        CURRENT_DATE,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '5 hours 30 minutes',
        1),

    -- Tomorrow
    ('trip_014', 'LINE-M2',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 6 hours',
        1),
    ('trip_015', 'LINE-M2',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 10 hours 30 minutes',
        1),
    ('trip_016', 'LINE-M2',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 15 hours 45 minutes',
        1),

    ('trip_017', 'LINE-5C',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 5 hours 30 minutes',
        1),
    ('trip_018', 'LINE-5C',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 9 hours',
        0),
    ('trip_019', 'LINE-5C',
        CURRENT_DATE + 1,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '1 day 13 hours 15 minutes',
        1),

    -- 2 days from now
    ('trip_020', 'LINE-M2',
        CURRENT_DATE + 2,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 days 7 hours',
        1),
    ('trip_021', 'LINE-M2',
        CURRENT_DATE + 2,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 days 11 hours 45 minutes',
        1),

    ('trip_022', 'LINE-5C',
        CURRENT_DATE + 2,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 days 6 hours 15 minutes',
        1),
    ('trip_023', 'LINE-5C',
        CURRENT_DATE + 2,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 days 12 hours 30 minutes',
        1),
    ('trip_024', 'LINE-5C',
        CURRENT_DATE + 2,
        (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') + INTERVAL '2 days 17 hours',
        1)
on conflict do nothing;


-- Add route_stops rows after deciding the key.
-- Add at least two trips per route on the same service date.
