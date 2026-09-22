begin;

alter table tickets
    alter column user_id set not null,
    alter column trip_id set not null,
    alter column product_code set not null,

    add constraint tickets_user_fk
        foreign key (user_id) references users(id)
        on delete restrict on update restrict,

    add constraint tickets_trip_fk
        foreign key (trip_id) references trips(id)
        on delete restrict on update restrict,

    add constraint tickets_product_fk
        foreign key (product_code) references products(code)
        on delete restrict on update restrict;

commit;
