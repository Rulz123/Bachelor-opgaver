begin;

alter table tickets
    alter column price set not null,
    alter column ticket_code set not null,
    alter column user_id set not null,
    add constraint tickets_price_non_negative
        check (price >= 0),
    add constraint tickets_code_unique
        unique (ticket_code),
    add constraint tickets_user_fk
        foreign key (user_id) references users(id);

commit;