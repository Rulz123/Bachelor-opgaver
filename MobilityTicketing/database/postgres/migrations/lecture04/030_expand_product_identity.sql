begin;
set local lock_timeout = '3s';

-- Assign an ID to each existing product.
alter table products add column id uuid;

update products
set id = gen_random_uuid()
where id is null;

-- Future products receive an ID automatically.
alter table products
    alter column id set default gen_random_uuid(),
    alter column id set not null,
    add constraint products_id_unique unique (id);

-- Tickets can gradually adopt the new reference.
alter table tickets
    add column product_id uuid;

alter table tickets
    add constraint tickets_product_id_fk
    foreign key (product_id)
    references products(id)
    not valid;

commit;