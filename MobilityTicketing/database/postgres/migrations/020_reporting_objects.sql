begin;

create table if not exists captured_revenue_daily (
    revenue_date date not null,
    currency text not null,
    captured_amount numeric not null,
    primary key (revenue_date, currency),
    constraint captured_revenue_daily_amount_valid
        check (captured_amount >= 0 and captured_amount < 'Infinity'::numeric),
    constraint captured_revenue_daily_currency_format
        check (currency ~ '^[A-Z]{3}$')
);

create or replace function captured_revenue_delta(
    payment_created_utc timestamptz,
    payment_amount numeric,
    payment_currency text,
    payment_status text
)
returns numeric
language sql
immutable
as $$
    select case when payment_status = 'Captured' then payment_amount else 0 end;
$$;

create or replace function apply_captured_revenue_payment_delta()
returns trigger
language plpgsql
as $$
declare
    old_delta numeric := 0;
    new_delta numeric := 0;
begin
    if tg_op <> 'INSERT' then
        old_delta := captured_revenue_delta(
            old.created_utc, old.amount, old.currency, old.status
        );
        if old_delta <> 0 then
            update captured_revenue_daily
            set captured_amount = captured_amount - old_delta
            where revenue_date = (old.created_utc at time zone 'UTC')::date
              and currency = old.currency;
        end if;
    end if;

    if tg_op <> 'DELETE' then
        new_delta := captured_revenue_delta(
            new.created_utc, new.amount, new.currency, new.status
        );
        if new_delta <> 0 then
            insert into captured_revenue_daily (revenue_date, currency, captured_amount)
            values ((new.created_utc at time zone 'UTC')::date, new.currency, new_delta)
            on conflict (revenue_date, currency) do update
            set captured_amount = captured_revenue_daily.captured_amount + excluded.captured_amount;
        end if;
    end if;

    if tg_op = 'DELETE' then
        return old;
    end if;
    return new;
end;
$$;

drop trigger if exists payments_captured_revenue_summary on payments;
create trigger payments_captured_revenue_summary
after insert or update or delete on payments
for each row execute function apply_captured_revenue_payment_delta();

create or replace function captured_revenue_for_day(
    requested_date date,
    requested_currency text default null
)
returns table (currency text, captured_amount numeric)
language sql
stable
as $$
    select p.currency, coalesce(sum(p.amount), 0)::numeric
    from payments p
    where (p.created_utc at time zone 'UTC')::date = requested_date
      and p.status = 'Captured'
      and (requested_currency is null or p.currency = requested_currency)
    group by p.currency
    order by p.currency;
$$;

drop materialized view if exists captured_revenue_daily_mv;
create materialized view captured_revenue_daily_mv as
select (p.created_utc at time zone 'UTC')::date as revenue_date,
       p.currency,
       sum(p.amount)::numeric as captured_amount
from payments p
where p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency;

create unique index if not exists captured_revenue_daily_mv_key
    on captured_revenue_daily_mv (revenue_date, currency);

insert into captured_revenue_daily (revenue_date, currency, captured_amount)
select (p.created_utc at time zone 'UTC')::date, p.currency, sum(p.amount)::numeric
from payments p
where p.status = 'Captured'
group by (p.created_utc at time zone 'UTC')::date, p.currency
on conflict (revenue_date, currency) do update
set captured_amount = excluded.captured_amount;

commit;