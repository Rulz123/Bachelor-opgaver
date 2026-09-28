\set ON_ERROR_STOP on

begin;

-- Deliberately pair the DAY product ID with the SINGLE code.
update tickets
set product_code = 'SINGLE'
where id = 'LAB04-NEW-1';

select
    t.id,
    t.product_code as stored_code,
    p.code as code_from_id,
    t.product_code = p.code as references_agree
from tickets t
join products p on p.id = t.product_id
where t.id = 'LAB04-NEW-1';

rollback;
