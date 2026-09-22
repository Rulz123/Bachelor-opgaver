begin;

alter table products
    add constraint products_currency_supported
        check (currency in ('DKK'));

alter table tickets
    add constraint tickets_currency_supported
        check (currency in ('DKK'));

alter table payments
    add constraint payments_currency_supported
        check (currency in ('DKK'));

commit;