begin;

alter table tickets
    alter column trip_id set not null,
    alter column product_code set not null,
    alter column currency set not null,
    alter column status set not null,
    alter column valid_from_utc set not null,
    alter column valid_to_utc set not null,

    add constraint tickets_trip_fk
        foreign key (trip_id) references trips(id),

    add constraint tickets_product_fk
        foreign key (product_code) references products(code),

    add constraint tickets_currency_format
        check (currency ~ '^[A-Z]{3}$'),

    add constraint tickets_status_allowed
        check (status in ('Active', 'Validated', 'Cancelled', 'Expired')),

    add constraint tickets_validity_order
        check (valid_to_utc >= valid_from_utc);

commit;