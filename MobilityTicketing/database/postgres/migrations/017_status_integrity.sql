begin;

alter table trips
    add constraint trips_status_valid
        check (status in ('scheduled', 'Scheduled'));

alter table tickets
    add constraint tickets_status_valid
        check (status in ('Active', 'Validated'));

alter table payments
    add constraint payments_status_valid
        check (status in ('Captured', 'Failed', 'Refunded'));

alter table validations
    add constraint validations_result_valid
        check (result in ('Accepted', 'Rejected'));

commit;