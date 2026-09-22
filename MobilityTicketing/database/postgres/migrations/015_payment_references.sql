begin;

alter table payments
    alter column ticket_id set not null,
    alter column user_id set not null,

    add constraint payments_ticket_fk
        foreign key (ticket_id) references tickets(id)
        on delete restrict on update restrict,

    add constraint payments_user_fk
        foreign key (user_id) references users(id)
        on delete restrict on update restrict,

    add constraint payments_external_reference_unique
        unique (external_payment_reference),

    add constraint payments_external_reference_not_blank
        check (
            external_payment_reference is null
            or length(btrim(external_payment_reference)) > 0
        );

commit;
