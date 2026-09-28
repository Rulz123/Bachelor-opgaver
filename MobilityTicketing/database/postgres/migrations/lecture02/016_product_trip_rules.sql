begin;

alter table products
    alter column name set not null,
    alter column price set not null,
    alter column currency set not null,

    add constraint products_price_non_negative
        check (price >= 0),

    add constraint products_currency_format
        check (currency ~ '^[A-Z]{3}$');

alter table trips
    add constraint trips_status_allowed
        check (status in ('Scheduled', 'Cancelled', 'Completed'));

commit;