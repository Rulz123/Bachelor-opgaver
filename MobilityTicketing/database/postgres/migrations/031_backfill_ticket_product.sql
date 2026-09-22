begin;

update tickets t
set product_id = p.product_id
from products p
where t.product_id is null
  and t.product_code = p.code;

do $$
begin
    if exists (select 1 from tickets where product_id is null) then
        raise exception 'ticket product backfill left unresolved rows';
    end if;
end;
$$;

commit;