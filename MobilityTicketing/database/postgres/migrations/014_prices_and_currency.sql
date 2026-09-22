begin;

alter table products
    alter column price set not null,
    alter column currency set not null,
    add constraint products_price_valid
        check (price >= 0 and price < 'Infinity'::numeric),
    add constraint products_currency_format
        check (currency ~ '^[A-Z]{3}$');

alter table tickets
    alter column price set not null,
    alter column currency set not null,
    add constraint tickets_price_valid
        check (price >= 0 and price < 'Infinity'::numeric),
    add constraint tickets_currency_format
        check (currency ~ '^[A-Z]{3}$');

alter table payments
    alter column amount set not null,
    alter column currency set not null,
    add constraint payments_amount_valid
        check (amount >= 0 and amount < 'Infinity'::numeric),
    add constraint payments_currency_format
        check (currency ~ '^[A-Z]{3}$');

commit;
