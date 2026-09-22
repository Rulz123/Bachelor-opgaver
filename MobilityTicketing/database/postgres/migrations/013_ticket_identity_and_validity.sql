begin;

alter table tickets
    alter column ticket_code set not null,
    alter column valid_from_utc set not null,
    alter column valid_to_utc set not null,

    add constraint tickets_code_unique
        unique (ticket_code),

    add constraint tickets_code_not_blank
        check (length(btrim(ticket_code)) > 0),

    add constraint tickets_validity_order
        check (valid_to_utc >= valid_from_utc);

commit;
