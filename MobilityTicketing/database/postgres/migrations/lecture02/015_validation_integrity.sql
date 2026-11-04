begin;

-- PostgreSQL needs a unique target for the two-column reference.
alter table tickets
    add constraint tickets_id_code_unique
        unique (id, ticket_code);

alter table validations
    alter column ticket_id set not null,
    alter column ticket_code set not null,
    alter column stop_id set not null,
    alter column result set not null,
    alter column validated_utc set not null,

    add constraint validations_ticket_identity_fk
        foreign key (ticket_id, ticket_code)
        references tickets(id, ticket_code),

    add constraint validations_stop_fk
        foreign key (stop_id) references stops(id),

    add constraint validations_result_allowed
        check (result in ('Accepted', 'Rejected'));

commit;