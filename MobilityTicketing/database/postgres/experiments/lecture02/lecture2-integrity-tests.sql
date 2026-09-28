-- Lecture 2 regression tests. Run after migrations 011 through 016.
-- Uses the unchanged course fixtures plus Lecture 1 trip TRIP-5C-001.
-- Each write is rolled back in an exception subtransaction.
-- SQLSTATE and named constraint/column must match; unexpected outcomes stop the run.
\set ON_ERROR_STOP on
\pset pager off
begin;
create temporary table integrity_test_results (
    test_no integer generated always as identity,
    test_name text, statement text, expected_state text, actual_state text,
    matched_rule text, outcome text
) on commit drop;

create function pg_temp.check_write(
    test_name text, statement text, expected_state text,
    expected_constraint text default null, expected_column text default null
) returns void language plpgsql as $$
declare
    actual_state text := '00000';
    actual_constraint text;
    actual_column text;
    affected bigint;
begin
    begin
        execute statement;
        get diagnostics affected = row_count;
        if affected <> 1 then
            raise exception 'Expected one affected row, got %', affected;
        end if;
        -- Force rollback even for a successful test write.
        raise exception using errcode = 'ZT001', message = 'Successful test write: undo it';
    exception
        when sqlstate 'ZT001' then
            actual_state := '00000';
        when others then
            get stacked diagnostics actual_state = returned_sqlstate,
                actual_constraint = constraint_name, actual_column = column_name;
    end;
    if actual_state <> expected_state
       or (expected_constraint is not null and actual_constraint is distinct from expected_constraint)
       or (expected_column is not null and actual_column is distinct from expected_column) then
        raise exception 'FAIL [%]: expected state %, constraint %, column %; got state %, constraint %, column %',
            test_name, expected_state, expected_constraint, expected_column,
            actual_state, actual_constraint, actual_column;
    end if;
    insert into integrity_test_results(test_name, statement, expected_state, actual_state, matched_rule, outcome)
    values(test_name, statement, expected_state, actual_state,
        coalesce(nullif(actual_constraint, ''), nullif(actual_column, ''), 'successful write'), 'PASS');
end;
$$;

do $tests$
begin
    perform pg_temp.check_write('trips.capacity: present value accepted', 'update trips set capacity = capacity where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('trips.capacity: NULL rejected', 'update trips set capacity = null where id = ''TRIP-5C-001''', '23502', null, 'capacity');
    perform pg_temp.check_write('trips.reserved_seats: present value accepted', 'update trips set reserved_seats = reserved_seats where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('trips.reserved_seats: NULL rejected', 'update trips set reserved_seats = null where id = ''TRIP-5C-001''', '23502', null, 'reserved_seats');
    perform pg_temp.check_write('tickets.price: present value accepted', 'update tickets set price = price where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.price: NULL rejected', 'update tickets set price = null where id = ''TICKET-1''', '23502', null, 'price');
    perform pg_temp.check_write('tickets.ticket_code: present value accepted', 'update tickets set ticket_code = ticket_code where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.ticket_code: NULL rejected', 'update tickets set ticket_code = null where id = ''TICKET-1''', '23502', null, 'ticket_code');
    perform pg_temp.check_write('tickets.user_id: present value accepted', 'update tickets set user_id = user_id where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.user_id: NULL rejected', 'update tickets set user_id = null where id = ''TICKET-1''', '23502', null, 'user_id');
    perform pg_temp.check_write('tickets.trip_id: present value accepted', 'update tickets set trip_id = trip_id where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.trip_id: NULL rejected', 'update tickets set trip_id = null where id = ''TICKET-1''', '23502', null, 'trip_id');
    perform pg_temp.check_write('tickets.product_code: present value accepted', 'update tickets set product_code = product_code where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.product_code: NULL rejected', 'update tickets set product_code = null where id = ''TICKET-1''', '23502', null, 'product_code');
    perform pg_temp.check_write('tickets.currency: present value accepted', 'update tickets set currency = currency where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.currency: NULL rejected', 'update tickets set currency = null where id = ''TICKET-1''', '23502', null, 'currency');
    perform pg_temp.check_write('tickets.status: present value accepted', 'update tickets set status = status where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.status: NULL rejected', 'update tickets set status = null where id = ''TICKET-1''', '23502', null, 'status');
    perform pg_temp.check_write('tickets.valid_from_utc: present value accepted', 'update tickets set valid_from_utc = valid_from_utc where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.valid_from_utc: NULL rejected', 'update tickets set valid_from_utc = null where id = ''TICKET-1''', '23502', null, 'valid_from_utc');
    perform pg_temp.check_write('tickets.valid_to_utc: present value accepted', 'update tickets set valid_to_utc = valid_to_utc where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.valid_to_utc: NULL rejected', 'update tickets set valid_to_utc = null where id = ''TICKET-1''', '23502', null, 'valid_to_utc');
    perform pg_temp.check_write('payments.user_id: present value accepted', 'update payments set user_id = user_id where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.user_id: NULL rejected', 'update payments set user_id = null where id = ''PAYMENT-1''', '23502', null, 'user_id');
    perform pg_temp.check_write('payments.ticket_id: present value accepted', 'update payments set ticket_id = ticket_id where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.ticket_id: NULL rejected', 'update payments set ticket_id = null where id = ''PAYMENT-1''', '23502', null, 'ticket_id');
    perform pg_temp.check_write('payments.external_payment_reference: present value accepted', 'update payments set external_payment_reference = external_payment_reference where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.external_payment_reference: NULL rejected', 'update payments set external_payment_reference = null where id = ''PAYMENT-1''', '23502', null, 'external_payment_reference');
    perform pg_temp.check_write('payments.amount: present value accepted', 'update payments set amount = amount where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.amount: NULL rejected', 'update payments set amount = null where id = ''PAYMENT-1''', '23502', null, 'amount');
    perform pg_temp.check_write('payments.currency: present value accepted', 'update payments set currency = currency where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.currency: NULL rejected', 'update payments set currency = null where id = ''PAYMENT-1''', '23502', null, 'currency');
    perform pg_temp.check_write('payments.status: present value accepted', 'update payments set status = status where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.status: NULL rejected', 'update payments set status = null where id = ''PAYMENT-1''', '23502', null, 'status');
    perform pg_temp.check_write('payments.created_utc: present value accepted', 'update payments set created_utc = created_utc where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.created_utc: NULL rejected', 'update payments set created_utc = null where id = ''PAYMENT-1''', '23502', null, 'created_utc');
    perform pg_temp.check_write('validations.ticket_id: present value accepted', 'update validations set ticket_id = ticket_id where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.ticket_id: NULL rejected', 'update validations set ticket_id = null where id = ''VALIDATION-1''', '23502', null, 'ticket_id');
    perform pg_temp.check_write('validations.ticket_code: present value accepted', 'update validations set ticket_code = ticket_code where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.ticket_code: NULL rejected', 'update validations set ticket_code = null where id = ''VALIDATION-1''', '23502', null, 'ticket_code');
    perform pg_temp.check_write('validations.stop_id: present value accepted', 'update validations set stop_id = stop_id where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.stop_id: NULL rejected', 'update validations set stop_id = null where id = ''VALIDATION-1''', '23502', null, 'stop_id');
    perform pg_temp.check_write('validations.result: present value accepted', 'update validations set result = result where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.result: NULL rejected', 'update validations set result = null where id = ''VALIDATION-1''', '23502', null, 'result');
    perform pg_temp.check_write('validations.validated_utc: present value accepted', 'update validations set validated_utc = validated_utc where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.validated_utc: NULL rejected', 'update validations set validated_utc = null where id = ''VALIDATION-1''', '23502', null, 'validated_utc');
    perform pg_temp.check_write('products.name: present value accepted', 'update products set name = name where code = ''SINGLE''', '00000', null, null);
    perform pg_temp.check_write('products.name: NULL rejected', 'update products set name = null where code = ''SINGLE''', '23502', null, 'name');
    perform pg_temp.check_write('products.price: present value accepted', 'update products set price = price where code = ''SINGLE''', '00000', null, null);
    perform pg_temp.check_write('products.price: NULL rejected', 'update products set price = null where code = ''SINGLE''', '23502', null, 'price');
    perform pg_temp.check_write('products.currency: present value accepted', 'update products set currency = currency where code = ''SINGLE''', '00000', null, null);
    perform pg_temp.check_write('products.currency: NULL rejected', 'update products set currency = null where code = ''SINGLE''', '23502', null, 'currency');
    perform pg_temp.check_write('Capacity lower bound: valid', 'update trips set capacity = 0, reserved_seats = 0 where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('Capacity lower bound: invalid', 'update trips set capacity = -1 where id = ''TRIP-5C-001''', '23514', 'trips_capacity_non_negative', null);
    perform pg_temp.check_write('Reservation lower bound: valid', 'update trips set reserved_seats = 0 where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('Reservation lower bound: invalid', 'update trips set reserved_seats = -1 where id = ''TRIP-5C-001''', '23514', 'trips_reserved_seats_valid', null);
    perform pg_temp.check_write('Reservation upper bound: valid', 'update trips set reserved_seats = capacity where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('Reservation upper bound: invalid', 'update trips set reserved_seats = capacity + 1 where id = ''TRIP-5C-001''', '23514', 'trips_reserved_seats_valid', null);
    perform pg_temp.check_write('tickets nonnegative amount: valid', 'update tickets set price = 0 where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets nonnegative amount: invalid', 'update tickets set price = -1 where id = ''TICKET-1''', '23514', 'tickets_price_non_negative', null);
    perform pg_temp.check_write('products nonnegative amount: valid', 'update products set price = 0 where code = ''SINGLE''', '00000', null, null);
    perform pg_temp.check_write('products nonnegative amount: invalid', 'update products set price = -1 where code = ''SINGLE''', '23514', 'products_price_non_negative', null);
    perform pg_temp.check_write('payments nonnegative amount: valid', 'update payments set amount = 0 where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments nonnegative amount: invalid', 'update payments set amount = -1 where id = ''PAYMENT-1''', '23514', 'payments_amount_non_negative', null);
    perform pg_temp.check_write('tickets currency format: valid', 'update tickets set currency = ''EUR'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets currency format: invalid', 'update tickets set currency = ''dkk'' where id = ''TICKET-1''', '23514', 'tickets_currency_format', null);
    perform pg_temp.check_write('payments currency format: valid', 'update payments set currency = ''EUR'' where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments currency format: invalid', 'update payments set currency = ''dkk'' where id = ''PAYMENT-1''', '23514', 'payments_currency_format', null);
    perform pg_temp.check_write('products currency format: valid', 'update products set currency = ''EUR'' where code = ''SINGLE''', '00000', null, null);
    perform pg_temp.check_write('products currency format: invalid', 'update products set currency = ''dkk'' where code = ''SINGLE''', '23514', 'products_currency_format', null);
    perform pg_temp.check_write('tickets allowed status: valid', 'update tickets set status = ''Expired'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets allowed status: invalid', 'update tickets set status = ''Banana'' where id = ''TICKET-1''', '23514', 'tickets_status_allowed', null);
    perform pg_temp.check_write('payments allowed status: valid', 'update payments set status = ''Refunded'' where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments allowed status: invalid', 'update payments set status = ''Banana'' where id = ''PAYMENT-1''', '23514', 'payments_status_allowed', null);
    perform pg_temp.check_write('trips allowed status: valid', 'update trips set status = ''Cancelled'' where id = ''TRIP-5C-001''', '00000', null, null);
    perform pg_temp.check_write('trips allowed status: invalid', 'update trips set status = ''Banana'' where id = ''TRIP-5C-001''', '23514', 'trips_status_allowed', null);
    perform pg_temp.check_write('validations allowed result: valid', 'update validations set result = ''Rejected'' where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations allowed result: invalid', 'update validations set result = ''Banana'' where id = ''VALIDATION-1''', '23514', 'validations_result_allowed', null);
    perform pg_temp.check_write('Validity order: valid', 'update tickets set valid_to_utc = valid_from_utc where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('Validity order: invalid', 'update tickets set valid_to_utc = valid_from_utc - interval ''1 minute'' where id = ''TICKET-1''', '23514', 'tickets_validity_order', null);
    perform pg_temp.check_write('tickets.user_id reference: valid', 'update tickets set user_id = ''USER-2'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.user_id reference: invalid', 'update tickets set user_id = ''__MISSING_TEST_REFERENCE__'' where id = ''TICKET-1''', '23503', 'tickets_user_fk', null);
    perform pg_temp.check_write('tickets.trip_id reference: valid', 'update tickets set trip_id = ''TRIP-5C-001'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.trip_id reference: invalid', 'update tickets set trip_id = ''__MISSING_TEST_REFERENCE__'' where id = ''TICKET-1''', '23503', 'tickets_trip_fk', null);
    perform pg_temp.check_write('tickets.product_code reference: valid', 'update tickets set product_code = ''DAY'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('tickets.product_code reference: invalid', 'update tickets set product_code = ''__MISSING_TEST_REFERENCE__'' where id = ''TICKET-1''', '23503', 'tickets_product_fk', null);
    perform pg_temp.check_write('payments.user_id reference: valid', 'update payments set user_id = ''USER-2'' where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.user_id reference: invalid', 'update payments set user_id = ''__MISSING_TEST_REFERENCE__'' where id = ''PAYMENT-1''', '23503', 'payments_user_fk', null);
    perform pg_temp.check_write('payments.ticket_id reference: valid', 'update payments set ticket_id = ''TICKET-2'' where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('payments.ticket_id reference: invalid', 'update payments set ticket_id = ''__MISSING_TEST_REFERENCE__'' where id = ''PAYMENT-1''', '23503', 'payments_ticket_fk', null);
    perform pg_temp.check_write('validations.stop_id reference: valid', 'update validations set stop_id = ''STOP-AIRPORT'' where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('validations.stop_id reference: invalid', 'update validations set stop_id = ''__MISSING_TEST_REFERENCE__'' where id = ''VALIDATION-1''', '23503', 'validations_stop_fk', null);
    perform pg_temp.check_write('Ticket code uniqueness: valid', 'update tickets set ticket_code = ''CODE-M2-0001'' where id = ''TICKET-1''', '00000', null, null);
    perform pg_temp.check_write('Ticket code uniqueness: invalid', 'update tickets set ticket_code = ''CODE-5C-0001'' where id = ''TICKET-1''', '23505', 'tickets_code_unique', null);
    perform pg_temp.check_write('Gateway reference uniqueness: valid', 'update payments set external_payment_reference = ''gateway-capture-0001'' where id = ''PAYMENT-1''', '00000', null, null);
    perform pg_temp.check_write('Gateway reference uniqueness: invalid', 'update payments set external_payment_reference = ''gateway-capture-0002'' where id = ''PAYMENT-1''', '23505', 'payments_external_reference_unique', null);
    perform pg_temp.check_write('Matching ticket identity: valid', 'update validations set ticket_id = ''TICKET-1'', ticket_code = ''CODE-M2-0001'' where id = ''VALIDATION-1''', '00000', null, null);
    perform pg_temp.check_write('Matching ticket identity: invalid', 'update validations set ticket_id = ''TICKET-1'' where id = ''VALIDATION-1''', '23503', 'validations_ticket_identity_fk', null);
    perform pg_temp.check_write('Referenced ticket deletion rejected', 'delete from tickets where id = ''TICKET-1''', '23503', 'payments_ticket_fk', null);
end;
$tests$;

select * from integrity_test_results order by test_no;
select count(*) as passed_tests from integrity_test_results;
rollback;
-- No test changes or helper objects persist.

