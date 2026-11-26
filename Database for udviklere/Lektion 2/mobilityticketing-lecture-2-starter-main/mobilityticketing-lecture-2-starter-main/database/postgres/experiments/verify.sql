\set ON_ERROR_STOP on
begin;
create function pg_temp.expect_failure(label text, statement text, expected_state text) returns void language plpgsql as $$
declare actual_state text;
begin
    begin
        execute statement;
    exception when others then
        get stacked diagnostics actual_state = returned_sqlstate;
    end;
    if actual_state is distinct from expected_state then
        raise exception '%: expected %, got %', label, expected_state, actual_state;
    end if;
    raise notice 'PASS % (%)', label, actual_state;
end $$;
select pg_temp.expect_failure('negative capacity', $$update trips set capacity=-1 where id='TRIP-M2-20260429-0800'$$, '23514');
select pg_temp.expect_failure('over capacity', $$update trips set reserved_seats=capacity+1 where id='TRIP-M2-20260429-0800'$$, '23514');
select pg_temp.expect_failure('unknown trip', $$update tickets set trip_id='MISSING' where id='TICKET-1'$$, '23503');
select pg_temp.expect_failure('reversed validity', $$update tickets set valid_to_utc=valid_from_utc-interval '1 second' where id='TICKET-1'$$, '23514');
select pg_temp.expect_failure('duplicate ticket code', $$insert into tickets select 'TEST-DUPLICATE',user_id,trip_id,ticket_code,status,product_code,valid_from_utc,valid_to_utc,price,currency from tickets where id='TICKET-1'$$, '23505');
select pg_temp.expect_failure('unknown status', $$update tickets set status='Unknown' where id='TICKET-1'$$, '23514');
select pg_temp.expect_failure('negative price', $$update products set price=-1 where code='SINGLE'$$, '23514');
select pg_temp.expect_failure('unknown payment ticket', $$update payments set ticket_id='MISSING' where external_payment_reference='gateway-capture-0001'$$, '23503');
select pg_temp.expect_failure('duplicate captured reference', $$insert into payments(id,user_id,ticket_id,external_payment_reference,amount,currency,status) values('TEST-DUPLICATE','USER-1','TICKET-1','gateway-capture-0001',36,'DKK','Captured')$$, '23505');
select pg_temp.expect_failure('mismatched ticket identity', $$insert into validations(id,ticket_id,ticket_code,vehicle_id,stop_id,device_id,result) values('TEST-MISMATCH','TICKET-1','CODE-5C-0001','BUS-5C-01','STOP-CENTRAL','DEVICE-01','Accepted')$$, '23503');
select pg_temp.expect_failure('invalid currency', $$update tickets set currency='dkk' where id='TICKET-1'$$, '23514');
insert into payments(id,user_id,ticket_id,external_payment_reference,amount,currency,status)
values('TEST-VALID','USER-1','TICKET-1','test-valid-reference',36,'DKK','Captured');
do $$ begin
    if not exists(select from payments where id='TEST-VALID') then raise exception 'Valid payment missing'; end if;
    raise notice 'PASS valid payment';
end $$;
rollback;
\echo 12 integrity checks passed; all test writes rolled back.