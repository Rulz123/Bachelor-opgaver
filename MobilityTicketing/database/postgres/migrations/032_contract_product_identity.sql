begin;

do $$
begin
    if exists (select 1 from tickets where product_id is null) then
        raise exception 'cannot contract product identity while tickets.product_id is null';
    end if;
end;
$$;

alter table tickets
    validate constraint tickets_product_id_fk;

alter table tickets
    alter column product_id set not null;

alter table tickets
    drop constraint tickets_product_fk;

alter table products
    add constraint products_code_unique unique (code);

alter table products
    drop constraint products_pkey,
    add primary key (product_id);

alter table tickets
    drop column product_code;

commit;