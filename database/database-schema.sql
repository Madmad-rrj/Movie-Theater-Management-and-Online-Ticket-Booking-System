-- Promethe-Phos: proposed physical baseline, reviewed 2026-11-09.
-- 37 logical relations + 4 ASP.NET Core Identity support tables = 41 tables.
-- Bootstrap EMPTY namespace only. NOT an upgrade migration of the old 18 tables.
-- Uses native range GiST operator classes; no btree_gist extension required.
-- Use psql -X -v ON_ERROR_STOP=1 -f database-schema.sql against an isolated database.
-- No DROP/reset; CREATE SCHEMA deliberately fails if cinema already exists.
BEGIN;
CREATE SCHEMA cinema;
REVOKE ALL ON SCHEMA cinema FROM PUBLIC;
SET LOCAL search_path = cinema, public;
SET LOCAL TIME ZONE 'UTC';
-- Unconstrained numeric + CHECK avoids silently rounding fractional VND at input.
CREATE DOMAIN vnd_amount AS numeric
  CHECK (VALUE >= 0 AND VALUE <= 99999999999999 AND VALUE = trunc(VALUE));

-- app_user
CREATE TABLE app_user (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_name varchar(256) NOT NULL,
    normalized_user_name varchar(256) NOT NULL UNIQUE,
    primary_email varchar(256) NOT NULL,
    normalized_email varchar(256) NOT NULL UNIQUE,
    email_confirmed boolean NOT NULL DEFAULT false,
    password_hash text,
    security_stamp text,
    concurrency_stamp text,
    phone_number text,
    phone_number_confirmed boolean NOT NULL DEFAULT false,
    two_factor_enabled boolean NOT NULL DEFAULT false,
    lockout_end timestamptz,
    lockout_enabled boolean NOT NULL DEFAULT true,
    access_failed_count integer NOT NULL DEFAULT 0 CHECK (access_failed_count >= 0),
    full_name varchar(150) NOT NULL CHECK (btrim(full_name) <> ''),
    account_state varchar(20) NOT NULL DEFAULT 'active' CHECK (account_state IN ('active','locked','deactivated')),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    CHECK (btrim(primary_email) <> '' AND btrim(normalized_email) <> '' AND btrim(normalized_user_name) <> '')
);


-- role
CREATE TABLE role (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE CHECK (code IN ('CUSTOMER','STAFF','MANAGER','ADMIN')),
    name varchar(256) NOT NULL,
    normalized_name varchar(256) NOT NULL UNIQUE,
    concurrency_stamp text
);


-- user_role
CREATE TABLE user_role (
    user_id bigint NOT NULL REFERENCES app_user(id),
    role_id bigint NOT NULL REFERENCES role(id),
    assigned_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    assigned_by_user_id bigint REFERENCES app_user(id),
    PRIMARY KEY (user_id,role_id)
);
CREATE INDEX ix_user_role_role ON user_role(role_id,user_id);

-- user_claim
CREATE TABLE user_claim (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES app_user(id),
    claim_type text,
    claim_value text
);
CREATE INDEX ix_user_claim_owner ON user_claim(user_id);

-- role_claim
CREATE TABLE role_claim (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    role_id bigint NOT NULL REFERENCES role(id),
    claim_type text,
    claim_value text
);
CREATE INDEX ix_role_claim_owner ON role_claim(role_id);

-- user_login
CREATE TABLE user_login (
    login_provider varchar(128) NOT NULL,
    provider_key varchar(128) NOT NULL,
    provider_display_name text,
    user_id bigint NOT NULL REFERENCES app_user(id),
    PRIMARY KEY (login_provider,provider_key)
);
CREATE INDEX ix_user_login_user ON user_login(user_id);

-- user_token
CREATE TABLE user_token (
    user_id bigint NOT NULL REFERENCES app_user(id),
    login_provider varchar(128) NOT NULL,
    name varchar(128) NOT NULL,
    value text,
    PRIMARY KEY (user_id,login_provider,name)
);


-- authenticated_session
CREATE TABLE authenticated_session (
    id uuid PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES app_user(id),
    started_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    revocation_reason text,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id,user_id),
    CHECK (expires_at > started_at),
    CHECK (revoked_at IS NULL OR revoked_at >= started_at)
);
CREATE UNIQUE INDEX ux_session_unrevoked ON authenticated_session(user_id) WHERE revoked_at IS NULL;

-- refresh_token
CREATE TABLE refresh_token (
    id uuid PRIMARY KEY,
    session_id uuid NOT NULL REFERENCES authenticated_session(id),
    token_hash text NOT NULL UNIQUE CHECK (btrim(token_hash) <> ''),
    created_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz,
    revoked_at timestamptz,
    replacement_token_id uuid,
    UNIQUE (id,session_id),
    FOREIGN KEY (replacement_token_id,session_id) REFERENCES refresh_token(id,session_id) DEFERRABLE INITIALLY DEFERRED,
    CHECK (expires_at > created_at),
    CHECK (replacement_token_id IS NULL OR (replacement_token_id <> id AND consumed_at IS NOT NULL))
);
CREATE INDEX ix_refresh_session ON refresh_token(session_id);

-- session_replacement_challenge
CREATE TABLE session_replacement_challenge (
    id uuid PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES app_user(id),
    expected_session_id uuid NOT NULL,
    challenge_hash text NOT NULL UNIQUE CHECK (btrim(challenge_hash) <> ''),
    created_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz,
    FOREIGN KEY (expected_session_id,user_id) REFERENCES authenticated_session(id,user_id),
    CHECK (expires_at > created_at)
);


-- email_verification_request
CREATE TABLE email_verification_request (
    id uuid PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES app_user(id),
    purpose varchar(24) NOT NULL CHECK (purpose IN ('initial_verification','email_change')),
    target_email varchar(256) NOT NULL CHECK (btrim(target_email) <> ''),
    normalized_target_email varchar(256) NOT NULL CHECK (btrim(normalized_target_email) <> ''),
    token_hash text NOT NULL UNIQUE CHECK (btrim(token_hash) <> ''),
    original_requested_at timestamptz NOT NULL,
    last_sent_at timestamptz NOT NULL,
    token_expires_at timestamptz NOT NULL,
    reservation_expires_at timestamptz,
    next_resend_at timestamptz NOT NULL,
    state varchar(16) NOT NULL DEFAULT 'active' CHECK (state IN ('active','verified','cancelled','expired','replaced')),
    consumed_at timestamptz,
    verified_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id,user_id,normalized_target_email),
    CHECK (last_sent_at >= original_requested_at AND token_expires_at > last_sent_at AND next_resend_at >= last_sent_at + interval '60 seconds'),
    CHECK ((purpose = 'initial_verification' AND reservation_expires_at IS NULL) OR (purpose = 'email_change' AND reservation_expires_at IS NOT NULL AND reservation_expires_at = original_requested_at + interval '72 hours' AND token_expires_at <= reservation_expires_at)),
    CHECK ((state = 'verified' AND consumed_at IS NOT NULL AND verified_at IS NOT NULL) OR (state <> 'verified' AND consumed_at IS NULL AND verified_at IS NULL))
);
CREATE UNIQUE INDEX ux_verification_active_user ON email_verification_request(user_id) WHERE state = 'active';
CREATE INDEX ix_verification_reservation_history ON email_verification_request(user_id,normalized_target_email,original_requested_at DESC);

-- email_address_claim
CREATE TABLE email_address_claim (
    normalized_email varchar(256) PRIMARY KEY CHECK (btrim(normalized_email) <> ''),
    user_id bigint NOT NULL REFERENCES app_user(id),
    verification_request_id uuid,
    claim_kind varchar(16) NOT NULL CHECK (claim_kind IN ('primary','reservation')),
    reserved_until timestamptz,
    FOREIGN KEY (verification_request_id,user_id,normalized_email) REFERENCES email_verification_request(id,user_id,normalized_target_email),
    CHECK ((claim_kind = 'primary' AND verification_request_id IS NULL AND reserved_until IS NULL) OR (claim_kind = 'reservation' AND verification_request_id IS NOT NULL AND reserved_until IS NOT NULL))
);
CREATE UNIQUE INDEX ux_primary_claim_user ON email_address_claim(user_id) WHERE claim_kind = 'primary';

-- checkout_identity
CREATE TABLE checkout_identity (
    id uuid PRIMARY KEY,
    user_id bigint REFERENCES app_user(id),
    guest_credential_hash text UNIQUE,
    guest_expires_at timestamptz,
    revoked_at timestamptz,
    created_at timestamptz NOT NULL,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    CHECK ((user_id IS NOT NULL AND guest_credential_hash IS NULL AND guest_expires_at IS NULL) OR (user_id IS NULL AND guest_credential_hash IS NOT NULL AND btrim(guest_credential_hash) <> '' AND guest_expires_at IS NOT NULL AND guest_expires_at > created_at))
);
CREATE UNIQUE INDEX ux_checkout_user ON checkout_identity(user_id) WHERE user_id IS NOT NULL;

-- movie
CREATE TABLE movie (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    seed_key varchar(100) UNIQUE,
    title varchar(255) NOT NULL CHECK (btrim(title) <> ''),
    description text,
    duration_minutes smallint NOT NULL CHECK (duration_minutes > 0),
    release_date date,
    release_market varchar(20),
    poster_url text,
    trailer_url text,
    language varchar(50),
    age_rating varchar(30),
    age_rating_system varchar(30),
    status varchar(20) NOT NULL DEFAULT 'coming_soon' CHECK (status IN ('coming_soon','now_showing','ended','archived')),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);
CREATE INDEX ix_movie_public_sort ON movie(release_date DESC NULLS LAST,id DESC) WHERE status IN ('coming_soon','now_showing');

-- genre
CREATE TABLE genre (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE CHECK (btrim(code) <> ''),
    name varchar(100) NOT NULL CHECK (btrim(name) <> ''),
    normalized_name varchar(100) NOT NULL UNIQUE CHECK (btrim(normalized_name) <> ''),
    description text,
    is_active boolean NOT NULL DEFAULT true,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);


-- movie_genre
CREATE TABLE movie_genre (
    movie_id bigint NOT NULL REFERENCES movie(id),
    genre_id bigint NOT NULL REFERENCES genre(id),
    PRIMARY KEY (movie_id,genre_id)
);
CREATE INDEX ix_movie_genre_reverse ON movie_genre(genre_id,movie_id);

-- cinema
CREATE TABLE cinema (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(40) NOT NULL UNIQUE CHECK (btrim(code) <> ''),
    name varchar(150) NOT NULL CHECK (btrim(name) <> ''),
    address text NOT NULL CHECK (btrim(address) <> ''),
    phone varchar(30),
    timezone varchar(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    status varchar(16) NOT NULL DEFAULT 'active' CHECK (status IN ('active','inactive')),
    minimum_turnaround_minutes smallint NOT NULL DEFAULT 15 CHECK (minimum_turnaround_minutes >= 0),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);


-- cinema_assignment
CREATE TABLE cinema_assignment (
    user_id bigint NOT NULL REFERENCES app_user(id),
    cinema_id bigint NOT NULL REFERENCES cinema(id),
    assignment_role varchar(16) NOT NULL CHECK (assignment_role IN ('STAFF','MANAGER')),
    assigned_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    assigned_by_user_id bigint NOT NULL REFERENCES app_user(id),
    PRIMARY KEY (user_id,cinema_id,assignment_role)
);
CREATE INDEX ix_assignment_cinema ON cinema_assignment(cinema_id,assignment_role,user_id);

-- cinema_room
CREATE TABLE cinema_room (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cinema_id bigint NOT NULL REFERENCES cinema(id),
    code varchar(30) NOT NULL CHECK (btrim(code) <> ''),
    name varchar(100) NOT NULL CHECK (btrim(name) <> ''),
    status varchar(16) NOT NULL DEFAULT 'active' CHECK (status IN ('active','inactive','maintenance')),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (cinema_id,code),
    UNIQUE (id,cinema_id)
);


-- seat_type
CREATE TABLE seat_type (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code varchar(30) NOT NULL UNIQUE CHECK (btrim(code) <> ''),
    name varchar(100) NOT NULL CHECK (btrim(name) <> ''),
    surcharge_amount vnd_amount NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);


-- seat
CREATE TABLE seat (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cinema_room_id bigint NOT NULL REFERENCES cinema_room(id),
    seat_type_id bigint NOT NULL REFERENCES seat_type(id),
    row_label varchar(10) NOT NULL CHECK (btrim(row_label) <> ''),
    seat_number smallint NOT NULL CHECK (seat_number > 0),
    x_position integer NOT NULL CHECK (x_position >= 0),
    y_position integer NOT NULL CHECK (y_position >= 0),
    operational_state varchar(20) NOT NULL DEFAULT 'active' CHECK (operational_state IN ('active','out_of_service')),
    retired_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (cinema_room_id,row_label,seat_number),
    UNIQUE (cinema_room_id,x_position,y_position),
    UNIQUE (id,cinema_room_id)
);


-- ticket_category
CREATE TABLE ticket_category (
    code varchar(16) PRIMARY KEY CHECK (code IN ('Adult','Discounted')),
    display_name varchar(100) NOT NULL CHECK (btrim(display_name) <> ''),
    eligibility_description text NOT NULL,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);


-- cinema_ticket_price
CREATE TABLE cinema_ticket_price (
    cinema_id bigint NOT NULL REFERENCES cinema(id),
    category_code varchar(16) NOT NULL REFERENCES ticket_category(code),
    amount vnd_amount NOT NULL,
    currency varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND'),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY (cinema_id,category_code)
);


-- showtime
CREATE TABLE showtime (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    seed_key varchar(120) UNIQUE,
    movie_id bigint NOT NULL REFERENCES movie(id),
    cinema_room_id bigint NOT NULL REFERENCES cinema_room(id),
    duration_minutes_snapshot smallint NOT NULL CHECK (duration_minutes_snapshot > 0),
    turnaround_minutes_applied smallint NOT NULL CHECK (turnaround_minutes_applied >= 0),
    starts_at timestamptz NOT NULL,
    ends_at timestamptz NOT NULL,
    room_available_at timestamptz NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','scheduled','in_progress','completed','cancelled')),
    cancelled_at timestamptz,
    cancellation_reason text,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id,cinema_room_id),
    CHECK (ends_at = starts_at + duration_minutes_snapshot * interval '1 minute'),
    CHECK (room_available_at = ends_at + turnaround_minutes_applied * interval '1 minute'),
    CHECK ((status = 'cancelled' AND cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '') OR (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL)),
    CONSTRAINT ex_showtime_room_time EXCLUDE USING gist (int8range(cinema_room_id,cinema_room_id,'[]') WITH &&, tstzrange(starts_at,room_available_at,'[)') WITH &&) WHERE (status <> 'cancelled')
);
CREATE INDEX ix_showtime_movie_time ON showtime(movie_id,starts_at,id);
CREATE INDEX ix_showtime_room_time ON showtime(cinema_room_id,starts_at,id);

-- showtime_change
CREATE TABLE showtime_change (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    showtime_id bigint NOT NULL REFERENCES showtime(id),
    old_starts_at timestamptz NOT NULL,
    new_starts_at timestamptz NOT NULL,
    changed_by_user_id bigint NOT NULL REFERENCES app_user(id),
    changed_at timestamptz NOT NULL,
    reason text NOT NULL CHECK (btrim(reason) <> ''),
    operation_key uuid NOT NULL,
    UNIQUE (showtime_id,operation_key),
    CHECK (old_starts_at <> new_starts_at)
);


-- selection_session
CREATE TABLE selection_session (
    id uuid PRIMARY KEY,
    checkout_identity_id uuid NOT NULL REFERENCES checkout_identity(id),
    showtime_id bigint NOT NULL,
    cinema_room_id bigint NOT NULL,
    started_at timestamptz NOT NULL,
    duration_seconds integer NOT NULL DEFAULT 300 CHECK (duration_seconds = 300),
    expires_at timestamptz NOT NULL,
    adult_quantity integer NOT NULL CHECK (adult_quantity >= 0),
    discounted_quantity integer NOT NULL CHECK (discounted_quantity >= 0),
    maximum_tickets_snapshot integer NOT NULL DEFAULT 8 CHECK (maximum_tickets_snapshot > 0),
    state varchar(16) NOT NULL DEFAULT 'active' CHECK (state IN ('active','consumed','expired','abandoned')),
    accepted_quote_fingerprint text,
    quoted_adult_price vnd_amount,
    quoted_discounted_price vnd_amount,
    quoted_total vnd_amount,
    quoted_showtime_start timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    FOREIGN KEY (showtime_id,cinema_room_id) REFERENCES showtime(id,cinema_room_id),
    UNIQUE (id,cinema_room_id),
    UNIQUE (id,checkout_identity_id,showtime_id,cinema_room_id),
    CHECK (expires_at = started_at + duration_seconds * interval '1 second'),
    CHECK (adult_quantity::bigint + discounted_quantity BETWEEN 1 AND maximum_tickets_snapshot)
);
CREATE UNIQUE INDEX ux_selection_active ON selection_session(checkout_identity_id,showtime_id) WHERE state = 'active';

-- selection_seat
CREATE TABLE selection_seat (
    selection_id uuid NOT NULL,
    seat_id bigint NOT NULL,
    cinema_room_id bigint NOT NULL,
    PRIMARY KEY (selection_id,seat_id),
    FOREIGN KEY (selection_id,cinema_room_id) REFERENCES selection_session(id,cinema_room_id),
    FOREIGN KEY (seat_id,cinema_room_id) REFERENCES seat(id,cinema_room_id)
);


-- booking
CREATE TABLE booking (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_code uuid NOT NULL UNIQUE,
    checkout_identity_id uuid NOT NULL REFERENCES checkout_identity(id),
    selection_session_id uuid NOT NULL UNIQUE,
    showtime_id bigint NOT NULL,
    cinema_room_id bigint NOT NULL,
    contact_full_name varchar(150) NOT NULL CHECK (btrim(contact_full_name) <> ''),
    contact_phone varchar(30) NOT NULL CHECK (btrim(contact_phone) <> ''),
    contact_email varchar(256) NOT NULL CHECK (btrim(contact_email) <> ''),
    terms_accepted_at timestamptz NOT NULL,
    age_acknowledged_at timestamptz NOT NULL,
    purchased_start_at timestamptz NOT NULL,
    movie_title_snapshot varchar(255) NOT NULL CHECK (btrim(movie_title_snapshot) <> ''),
    room_name_snapshot varchar(100) NOT NULL CHECK (btrim(room_name_snapshot) <> ''),
    reservation_expires_at timestamptz NOT NULL,
    maximum_tickets_snapshot integer NOT NULL DEFAULT 8 CHECK (maximum_tickets_snapshot > 0),
    total_amount vnd_amount NOT NULL,
    currency varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND'),
    status varchar(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','confirmed','expired','cancelled')),
    created_at timestamptz NOT NULL,
    confirmed_at timestamptz,
    cancelled_at timestamptz,
    cancellation_reason text,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id,showtime_id,cinema_room_id),
    UNIQUE (id,checkout_identity_id),
    FOREIGN KEY (showtime_id,cinema_room_id) REFERENCES showtime(id,cinema_room_id),
    FOREIGN KEY (selection_session_id,checkout_identity_id,showtime_id,cinema_room_id) REFERENCES selection_session(id,checkout_identity_id,showtime_id,cinema_room_id),
    CHECK (reservation_expires_at > created_at),
    CHECK ((status = 'confirmed' AND confirmed_at IS NOT NULL) OR (status IN ('pending','expired') AND confirmed_at IS NULL) OR status = 'cancelled'),
    CHECK ((status = 'cancelled' AND cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '') OR (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL))
);
CREATE INDEX ix_booking_owner_history ON booking(checkout_identity_id,created_at DESC,id DESC);
CREATE INDEX ix_booking_showtime_state ON booking(showtime_id,status,id);
CREATE INDEX ix_booking_pending_expiry ON booking(reservation_expires_at,id) WHERE status = 'pending';

-- booking_category
CREATE TABLE booking_category (
    booking_id bigint NOT NULL REFERENCES booking(id),
    category_code varchar(16) NOT NULL REFERENCES ticket_category(code),
    quantity integer NOT NULL CHECK (quantity >= 0),
    base_price_snapshot vnd_amount NOT NULL,
    category_name_snapshot varchar(100) NOT NULL CHECK (btrim(category_name_snapshot) <> ''),
    admitted_quantity integer NOT NULL DEFAULT 0,
    PRIMARY KEY (booking_id,category_code),
    CHECK (admitted_quantity BETWEEN 0 AND quantity)
);


-- booking_seat
CREATE TABLE booking_seat (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL,
    showtime_id bigint NOT NULL,
    cinema_room_id bigint NOT NULL,
    seat_id bigint NOT NULL,
    seat_label_snapshot varchar(30) NOT NULL CHECK (btrim(seat_label_snapshot) <> ''),
    seat_type_name_snapshot varchar(100) NOT NULL CHECK (btrim(seat_type_name_snapshot) <> ''),
    surcharge_snapshot vnd_amount NOT NULL,
    allocation_status varchar(16) NOT NULL DEFAULT 'held' CHECK (allocation_status IN ('held','sold','released')),
    released_at timestamptz,
    UNIQUE (booking_id,seat_id),
    FOREIGN KEY (booking_id,showtime_id,cinema_room_id) REFERENCES booking(id,showtime_id,cinema_room_id),
    FOREIGN KEY (seat_id,cinema_room_id) REFERENCES seat(id,cinema_room_id),
    CHECK ((allocation_status = 'released') = (released_at IS NOT NULL))
);
CREATE UNIQUE INDEX ux_active_seat ON booking_seat(showtime_id,seat_id) WHERE allocation_status IN ('held','sold');

-- operation_receipt
CREATE TABLE operation_receipt (
    id uuid PRIMARY KEY,
    actor_scope text NOT NULL CHECK (btrim(actor_scope) <> ''),
    operation_name varchar(80) NOT NULL CHECK (btrim(operation_name) <> ''),
    target_scope text NOT NULL CHECK (btrim(target_scope) <> ''),
    request_key uuid NOT NULL,
    request_fingerprint text NOT NULL CHECK (btrim(request_fingerprint) <> ''),
    processing_state varchar(16) NOT NULL CHECK (processing_state IN ('processing','committed','rejected')),
    result_reference text,
    created_at timestamptz NOT NULL,
    committed_at timestamptz,
    lease_until timestamptz,
    claim_token uuid,
    UNIQUE (actor_scope,operation_name,target_scope,request_key),
    CHECK ((processing_state = 'committed') = (committed_at IS NOT NULL)),
    CHECK ((lease_until IS NULL) = (claim_token IS NULL))
);


-- payment
CREATE TABLE payment (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL REFERENCES booking(id),
    provider varchar(30) NOT NULL CHECK (btrim(provider) <> ''),
    merchant_request_id uuid NOT NULL UNIQUE,
    provider_transaction_id text,
    amount vnd_amount NOT NULL CHECK (amount > 0),
    currency varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND'),
    received_amount numeric,
    received_currency varchar(12),
    status varchar(16) NOT NULL DEFAULT 'created' CHECK (status IN ('created','pending','unknown','succeeded','failed','cancelled')),
    created_at timestamptz NOT NULL,
    initiation_committed_at timestamptz NOT NULL,
    initiated_at timestamptz,
    payment_window_seconds integer NOT NULL DEFAULT 600 CHECK (payment_window_seconds = 600),
    payment_deadline timestamptz,
    grace_seconds integer NOT NULL DEFAULT 10 CHECK (grace_seconds >= 0),
    grace_until timestamptz,
    verified_at timestamptz,
    paid_at timestamptz,
    applied_at timestamptz,
    reconciliation_status varchar(24) NOT NULL DEFAULT 'none' CHECK (reconciliation_status IN ('none','needs_review','refund_required','resolved')),
    next_reconcile_at timestamptz,
    retry_count integer NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    lease_until timestamptz,
    claim_token uuid,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id,booking_id),
    UNIQUE (id,provider),
    CHECK (provider_transaction_id IS NULL OR btrim(provider_transaction_id) <> ''),
    CHECK (received_amount IS NULL OR (received_amount >= 0 AND received_amount < 1e18)),
    CHECK ((initiated_at IS NULL AND payment_deadline IS NULL AND grace_until IS NULL) OR (initiated_at IS NOT NULL AND payment_deadline IS NOT NULL AND grace_until IS NOT NULL AND payment_deadline = initiated_at + payment_window_seconds * interval '1 second' AND grace_until = payment_deadline + grace_seconds * interval '1 second')),
    CHECK (status <> 'succeeded' OR (verified_at IS NOT NULL AND paid_at IS NOT NULL AND received_amount IS NOT NULL AND received_currency IS NOT NULL AND provider_transaction_id IS NOT NULL)),
    CHECK (applied_at IS NULL OR (status = 'succeeded' AND received_amount = amount AND received_currency = currency)),
    CHECK ((lease_until IS NULL) = (claim_token IS NULL))
);
CREATE UNIQUE INDEX ux_payment_open ON payment(booking_id) WHERE status IN ('created','pending','unknown');
CREATE UNIQUE INDEX ux_payment_applied ON payment(booking_id) WHERE applied_at IS NOT NULL;
CREATE UNIQUE INDEX ux_payment_provider_tx ON payment(provider,provider_transaction_id) WHERE provider_transaction_id IS NOT NULL;
CREATE INDEX ix_payment_reconcile ON payment(next_reconcile_at,id) WHERE status IN ('created','pending','unknown') OR reconciliation_status IN ('needs_review','refund_required');

-- payment_event
CREATE TABLE payment_event (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    payment_id bigint,
    provider varchar(30) NOT NULL CHECK (btrim(provider) <> ''),
    provider_event_identity text NOT NULL CHECK (btrim(provider_event_identity) <> ''),
    source varchar(16) NOT NULL CHECK (source IN ('webhook','query')),
    sanitized_payload jsonb NOT NULL,
    received_at timestamptz NOT NULL,
    verified_at timestamptz NOT NULL,
    processing_status varchar(16) NOT NULL DEFAULT 'received' CHECK (processing_status IN ('received','processed','failed','needs_review')),
    processed_at timestamptz,
    retry_count integer NOT NULL DEFAULT 0 CHECK (retry_count >= 0),
    next_retry_at timestamptz,
    lease_until timestamptz,
    claim_token uuid,
    last_error_code varchar(100),
    UNIQUE (provider,provider_event_identity),
    FOREIGN KEY (payment_id,provider) REFERENCES payment(id,provider),
    CHECK ((processing_status = 'processed') = (processed_at IS NOT NULL)),
    CHECK ((lease_until IS NULL) = (claim_token IS NULL))
);
CREATE INDEX ix_event_due ON payment_event(next_retry_at,id) WHERE processing_status IN ('received','failed');

-- ticket
CREATE TABLE ticket (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL UNIQUE REFERENCES booking(id),
    ticket_code uuid NOT NULL UNIQUE,
    qr_lookup_hash text NOT NULL UNIQUE CHECK (btrim(qr_lookup_hash) <> ''),
    encrypted_qr_credential bytea NOT NULL CHECK (octet_length(encrypted_qr_credential) > 0),
    qr_key_version varchar(60) NOT NULL CHECK (btrim(qr_key_version) <> ''),
    issued_at timestamptz NOT NULL,
    status varchar(16) NOT NULL DEFAULT 'valid' CHECK (status IN ('valid','voided')),
    voided_at timestamptz,
    void_reason text,
    UNIQUE (id,booking_id),
    CHECK ((status = 'voided' AND voided_at IS NOT NULL AND void_reason IS NOT NULL AND btrim(void_reason) <> '') OR (status = 'valid' AND voided_at IS NULL AND void_reason IS NULL))
);


-- admission_operation
CREATE TABLE admission_operation (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL REFERENCES booking(id),
    ticket_id bigint NOT NULL,
    staff_user_id bigint NOT NULL REFERENCES app_user(id),
    request_key uuid NOT NULL,
    fingerprint text NOT NULL CHECK (btrim(fingerprint) <> ''),
    adult_quantity integer NOT NULL CHECK (adult_quantity >= 0),
    discounted_quantity integer NOT NULL CHECK (discounted_quantity >= 0),
    age_verified boolean NOT NULL CHECK (age_verified),
    discounted_eligibility_verified boolean NOT NULL,
    official_start_at_validation timestamptz NOT NULL,
    admitted_at timestamptz NOT NULL,
    UNIQUE (booking_id,request_key),
    FOREIGN KEY (ticket_id,booking_id) REFERENCES ticket(id,booking_id),
    CHECK (adult_quantity::bigint + discounted_quantity > 0),
    CHECK (discounted_quantity = 0 OR discounted_eligibility_verified),
    CHECK (admitted_at BETWEEN official_start_at_validation - interval '5 minutes' AND official_start_at_validation + interval '20 minutes')
);


-- cancellation_request
CREATE TABLE cancellation_request (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id bigint NOT NULL,
    requested_by_identity_id uuid NOT NULL,
    request_key uuid NOT NULL,
    requested_at timestamptz NOT NULL,
    purchased_start_snapshot timestamptz NOT NULL,
    official_start_at_request timestamptz NOT NULL,
    reason text NOT NULL CHECK (btrim(reason) <> ''),
    status varchar(32) NOT NULL DEFAULT 'pending_review' CHECK (status IN ('pending_review','approved_awaiting_refund','rejected','withdrawn','completed')),
    reviewed_by_user_id bigint REFERENCES app_user(id),
    reviewed_at timestamptz,
    decision_reason text,
    withdrawn_at timestamptz,
    completed_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (requested_by_identity_id,request_key),
    UNIQUE (id,booking_id),
    FOREIGN KEY (booking_id,requested_by_identity_id) REFERENCES booking(id,checkout_identity_id),
    CHECK (purchased_start_snapshot <> official_start_at_request AND requested_at < official_start_at_request),
    CHECK ((status = 'withdrawn') = (withdrawn_at IS NOT NULL)),
    CHECK ((status = 'completed') = (completed_at IS NOT NULL)),
    CHECK (status NOT IN ('approved_awaiting_refund','completed') OR (reviewed_at IS NOT NULL AND reviewed_by_user_id IS NOT NULL)),
    CHECK (status <> 'rejected' OR (reviewed_at IS NOT NULL AND decision_reason IS NOT NULL AND btrim(decision_reason) <> ''))
);
CREATE UNIQUE INDEX ux_cancellation_open ON cancellation_request(booking_id) WHERE status IN ('pending_review','approved_awaiting_refund');

-- refund
CREATE TABLE refund (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    payment_id bigint NOT NULL UNIQUE,
    booking_id bigint NOT NULL REFERENCES booking(id),
    cancellation_request_id bigint UNIQUE,
    origin varchar(30) NOT NULL CHECK (origin IN ('payment_reconciliation','showtime_rescheduling')),
    merchant_refund_id uuid NOT NULL UNIQUE,
    amount numeric NOT NULL CHECK (amount > 0 AND amount < 1e18),
    currency varchar(12) NOT NULL CHECK (btrim(currency) <> ''),
    status varchar(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','unknown','failed','succeeded')),
    created_at timestamptz NOT NULL,
    completed_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    FOREIGN KEY (payment_id,booking_id) REFERENCES payment(id,booking_id),
    FOREIGN KEY (cancellation_request_id,booking_id) REFERENCES cancellation_request(id,booking_id),
    CHECK ((origin = 'showtime_rescheduling' AND cancellation_request_id IS NOT NULL) OR (origin = 'payment_reconciliation' AND cancellation_request_id IS NULL)),
    CHECK ((status = 'succeeded') = (completed_at IS NOT NULL))
);


-- refund_completion_evidence
CREATE TABLE refund_completion_evidence (
    refund_id bigint PRIMARY KEY REFERENCES refund(id),
    transfer_channel varchar(80) NOT NULL CHECK (btrim(transfer_channel) <> ''),
    external_reference text NOT NULL CHECK (btrim(external_reference) <> ''),
    amount numeric NOT NULL CHECK (amount > 0 AND amount < 1e18),
    currency varchar(12) NOT NULL CHECK (btrim(currency) <> ''),
    externally_completed_at timestamptz NOT NULL,
    evidence_note text NOT NULL CHECK (btrim(evidence_note) <> ''),
    recorded_by_user_id bigint NOT NULL REFERENCES app_user(id),
    recorded_at timestamptz NOT NULL,
    operation_key uuid NOT NULL UNIQUE,
    UNIQUE (transfer_channel,external_reference),
    CHECK (recorded_at >= externally_completed_at)
);


-- notification_request
CREATE TABLE notification_request (
    id uuid PRIMARY KEY,
    deduplication_key text NOT NULL UNIQUE CHECK (btrim(deduplication_key) <> ''),
    notification_kind varchar(30) NOT NULL CHECK (notification_kind IN ('ticket_issued','ticket_resend','verification','email_change_security','showtime_changed')),
    booking_id bigint REFERENCES booking(id),
    verification_request_id uuid REFERENCES email_verification_request(id),
    recipient_email varchar(256) NOT NULL CHECK (btrim(recipient_email) <> ''),
    template_key varchar(100) NOT NULL CHECK (btrim(template_key) <> ''),
    protected_payload bytea,
    payload_key_version varchar(60),
    state varchar(16) NOT NULL DEFAULT 'pending' CHECK (state IN ('pending','sending','sent','failed','cancelled')),
    created_at timestamptz NOT NULL,
    next_attempt_at timestamptz,
    lease_until timestamptz,
    claim_token uuid,
    sent_at timestamptz,
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    CHECK ((notification_kind IN ('ticket_issued','ticket_resend','showtime_changed') AND booking_id IS NOT NULL AND verification_request_id IS NULL) OR (notification_kind IN ('verification','email_change_security') AND booking_id IS NULL AND verification_request_id IS NOT NULL)),
    CHECK ((protected_payload IS NULL) = (payload_key_version IS NULL)),
    CHECK ((lease_until IS NULL) = (claim_token IS NULL)),
    CHECK ((state = 'sent') = (sent_at IS NOT NULL))
);
CREATE INDEX ix_notification_due ON notification_request(next_attempt_at,id) WHERE state IN ('pending','sending','failed');

-- notification_attempt
CREATE TABLE notification_attempt (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    request_id uuid NOT NULL REFERENCES notification_request(id),
    attempt_no integer NOT NULL CHECK (attempt_no > 0),
    claim_token uuid NOT NULL,
    started_at timestamptz NOT NULL,
    finished_at timestamptz,
    provider_message_id text,
    outcome varchar(16) NOT NULL CHECK (outcome IN ('started','sent','failed','unknown')),
    sanitized_error_code varchar(100),
    UNIQUE (request_id,attempt_no),
    CHECK ((outcome = 'started' AND finished_at IS NULL) OR (outcome <> 'started' AND finished_at IS NOT NULL))
);


-- audit_event
CREATE TABLE audit_event (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    operation_identity uuid NOT NULL,
    event_type varchar(100) NOT NULL CHECK (btrim(event_type) <> ''),
    actor_kind varchar(16) NOT NULL CHECK (actor_kind IN ('user','guest','system','provider')),
    actor_user_id bigint REFERENCES app_user(id),
    actor_scope text,
    resource_type varchar(80) NOT NULL CHECK (btrim(resource_type) <> ''),
    resource_id text NOT NULL CHECK (btrim(resource_id) <> ''),
    occurred_at timestamptz NOT NULL,
    correlation_id text,
    sanitized_context jsonb NOT NULL DEFAULT '{}'::jsonb,
    UNIQUE (operation_identity,event_type,resource_type,resource_id),
    CHECK ((actor_kind = 'user') = (actor_user_id IS NOT NULL))
);
CREATE INDEX ix_audit_resource ON audit_event(resource_type,resource_id,occurred_at);

-- Required category rows belong to schema bootstrap, not optional demo data.
INSERT INTO ticket_category(code,display_name,eligibility_description) VALUES
('Adult','Adult','Standard category'),
('Discounted','Discounted','Students, teachers, and seniors aged 60 or above; eligibility checked at admission');

CREATE FUNCTION cinema.reject_history_mutation() RETURNS trigger
LANGUAGE plpgsql SET search_path = pg_catalog AS $$
BEGIN RAISE EXCEPTION 'Append-only relation: %', TG_TABLE_NAME USING ERRCODE = '23514'; END;
$$;
CREATE TRIGGER tr_admission_operation_append_only BEFORE UPDATE OR DELETE ON cinema.admission_operation FOR EACH ROW EXECUTE FUNCTION cinema.reject_history_mutation();
CREATE TRIGGER tr_refund_completion_evidence_append_only BEFORE UPDATE OR DELETE ON cinema.refund_completion_evidence FOR EACH ROW EXECUTE FUNCTION cinema.reject_history_mutation();
CREATE TRIGGER tr_audit_event_append_only BEFORE UPDATE OR DELETE ON cinema.audit_event FOR EACH ROW EXECUTE FUNCTION cinema.reject_history_mutation();
CREATE TRIGGER tr_showtime_change_append_only BEFORE UPDATE OR DELETE ON cinema.showtime_change FOR EACH ROW EXECUTE FUNCTION cinema.reject_history_mutation();
CREATE TRIGGER tr_category_no_delete BEFORE DELETE ON cinema.ticket_category FOR EACH ROW EXECUTE FUNCTION cinema.reject_history_mutation();
REVOKE ALL ON ALL TABLES IN SCHEMA cinema FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA cinema FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA cinema FROM PUBLIC;
COMMIT;
-- Runtime role grants and EF fluent mapping are deployment tasks, not automatic here.
-- RLS is not configured: never expose this schema directly to browser/anon roles.
