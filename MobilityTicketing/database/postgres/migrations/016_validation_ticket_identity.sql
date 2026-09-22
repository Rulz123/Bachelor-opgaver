begin;

alter table tickets
    add constraint tickets_id_code_unique
        unique (id, ticket_code);

alter table validations
    alter column ticket_id set not null,
    alter column ticket_code set not null,

    add constraint validations_ticket_identity_fk
        foreign key (ticket_id, ticket_code)
        references tickets (id, ticket_code)
        on delete restrict on update restrict;

commit;
