begin;

alter table payments
    alter column user_id set not null,
    alter column ticket_id set not null,
    alter column external_payment_reference set not null,
    alter column amount set not null,
    alter column currency set not null,
    alter column status set not null,
    alter column created_utc set not null,

    add constraint payments_user_fk
        foreign key (user_id) references users(id),

    add constraint payments_ticket_fk
        foreign key (ticket_id) references tickets(id),

    add constraint payments_amount_non_negative
        check (amount >= 0),

    add constraint payments_currency_format
        check (currency ~ '^[A-Z]{3}$'),

    add constraint payments_status_allowed
        check (status in ('Pending', 'Captured', 'Failed', 'Refunded')),

    add constraint payments_external_reference_unique
        unique (external_payment_reference);

commit;