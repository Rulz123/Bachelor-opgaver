\set ON_ERROR_STOP 1

begin;

\echo 'Expand phase: create an isolated compatibility copy'
create temp table products_compat as
select product_id, code, name, price, currency
from products;
alter table products_compat add primary key (product_id);
alter table products_compat add unique (code);

create temp table tickets_compat as
select t.id, p.code as product_code, t.product_id, t.price, t.currency
from tickets t
join products p on p.product_id = t.product_id;
alter table tickets_compat add primary key (id);
alter table tickets_compat add constraint tickets_compat_product_fk
    foreign key (product_id) references products_compat(product_id);

\echo 'Old reader: reads the preserved product_code representation'
select id, product_code, price, currency
from tickets_compat
order by id;

\echo 'New reader: joins tickets to products through product_id'
select t.id, p.code, p.product_id, t.price, t.currency
from tickets_compat t
join products_compat p on p.product_id = t.product_id
order by t.id;

\echo 'New writer: writes product_id and keeps compatibility code explicitly synchronized'
insert into tickets_compat (id, product_code, product_id, price, currency)
select 'COMPAT-NEW-WRITER', p.code, p.product_id, p.price, p.currency
from products_compat p
where p.code = 'DAY';

\echo 'Old writer during overlap: writes only the legacy product_code'
insert into tickets_compat (id, product_code, price, currency)
select 'COMPAT-LATE-OLD-WRITER', code, price, currency
from products_compat
where code = 'SINGLE';

\echo 'Backfill phase: resolve the late old-writer row'
update tickets_compat t
set product_id = p.product_id
from products_compat p
where t.product_id is null
  and t.product_code = p.code;

select id, product_code, product_id
from tickets_compat
where id in ('COMPAT-NEW-WRITER', 'COMPAT-LATE-OLD-WRITER')
order by id;

\echo 'Compatibility and price/currency preservation checks'
select count(*) as unresolved_rows
from tickets_compat
where product_id is null;

select t.id,
       t.price as ticket_price,
       p.price as product_price,
       t.currency as ticket_currency,
       p.currency as product_currency
from tickets_compat t
join products_compat p on p.product_id = t.product_id
where t.id in ('COMPAT-NEW-WRITER', 'COMPAT-LATE-OLD-WRITER')
order by t.id;

\echo 'Pre-contract verification: inspect final-schema dependencies before contraction'
select conname, conrelid::regclass as table_name, confrelid::regclass as referenced_table
from pg_constraint
where conname in ('tickets_product_id_fk', 'products_pkey', 'products_code_unique')
order by conname;

\echo 'Contract preconditions: all compatibility rows resolve before NOT NULL'
select count(*) filter (where product_id is null) as null_product_ids,
       count(*) as compatibility_rows
from tickets_compat;

rollback;