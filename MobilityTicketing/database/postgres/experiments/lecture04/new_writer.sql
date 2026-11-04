\set ON_ERROR_STOP on

insert into tickets (
    id, user_id, trip_id, ticket_code, status,
    product_code, product_id,
    valid_from_utc, valid_to_utc, price, currency
)
values (
    :'ticket_id',
    'USER-1',
    'TRIP-M2-20260429-1200',
    :'ticket_code',
    'Active',
    (select code from products where id = :'product_id'::uuid),
    :'product_id'::uuid,
    '2026-04-29 00:00:00+00',
    '2026-04-30 00:00:00+00',
    :agreed_price,
    :'agreed_currency'
)
returning id, product_code, product_id, price, currency;