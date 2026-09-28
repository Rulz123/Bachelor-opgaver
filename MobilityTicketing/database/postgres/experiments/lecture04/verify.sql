\set ON_ERROR_STOP on

\echo REFERENCE PROBLEMS -- expect zero rows
select t.id, t.product_code, t.product_id
from tickets t
left join products p on p.id = t.product_id
where t.product_id is null
   or p.id is null
   or t.product_code is distinct from p.code;

\echo ORIGINAL TICKET CHANGES -- expect zero rows
with baseline(id, product_code, price, currency) as (
    values
        ('TICKET-1', 'SINGLE', 36.00, 'DKK'),
        ('TICKET-2', 'SINGLE', 36.00, 'DKK'),
        ('TICKET-3', 'DAY',    65.00, 'DKK')
)
select
    b.id,
    b.product_code as expected_product,
    p.code as actual_product,
    b.price as expected_price,
    t.price as actual_price,
    b.currency as expected_currency,
    t.currency as actual_currency
from baseline b
left join tickets t on t.id = b.id
left join products p on p.id = t.product_id
where t.id is null
   or p.code is distinct from b.product_code
   or t.price is distinct from b.price
   or t.currency is distinct from b.currency;