-- =====================================================================
-- Movie Theater Management and Online Ticket Booking System
-- Database schema - PostgreSQL 15+ (Supabase)
-- File: database/database-schema.sql
--
-- Nguon dac ta: database/database-schema.md (18 relation).
-- Quy uoc:
--   * PK don: bigint GENERATED ALWAYS AS IDENTITY; FK: bigint
--   * Thoi gian: timestamptz | Tien VND: numeric(14,0) | Tien cong thanh toan/hoan: numeric(18,2)
--   * Trang thai: varchar + CHECK, gia tri chu thuong
--   * Chuoi bat buoc khong rong sau btrim; unique ten/email theo lower(btrim(x))
--   * FK mac dinh ON UPDATE RESTRICT / ON DELETE RESTRICT (tru bang noi ghi ro)
--   * Ten: pk_ / fk_ / uq_ / ck_ / ex_ (constraint), ux_ (unique index), ix_ (index)
--
-- Chay:  psql -v ON_ERROR_STOP=1 -f database/database-schema.sql
-- File nay chi TAO moi, khong DROP. De reset khi dev, xem khoi comment ngay duoi.
-- =====================================================================

-- ---------------------------------------------------------------------
-- (DEV ONLY) Reset toan bo schema. Bo comment khi can chay lai tu dau.
-- KHONG dung tren database that / Supabase dang co du lieu.
-- ---------------------------------------------------------------------
-- DROP TABLE IF EXISTS refund, payment_event, payment, ticket, booking_detail, booking,
--     user_role, role, app_user, showtime, ticket_type, seat, seat_type,
--     cinema_room, cinema, movie_genre, genre, movie CASCADE;

BEGIN;

CREATE EXTENSION IF NOT EXISTS btree_gist;  -- can cho ex_showtime_room_time

-- =====================================================================
-- 1. PHIM & THE LOAI
-- =====================================================================
CREATE TABLE movie (
    id                bigint GENERATED ALWAYS AS IDENTITY,
    title             varchar(255) NOT NULL,
    description       text,
    duration_minutes  smallint     NOT NULL,
    release_date      date,
    poster_url        text,
    trailer_url       text,
    language          varchar(50),
    age_rating        varchar(20),
    status            varchar(20)  NOT NULL DEFAULT 'coming_soon',
    CONSTRAINT pk_movie PRIMARY KEY (id),
    CONSTRAINT ck_movie_title_nonblank CHECK (btrim(title) <> ''),
    CONSTRAINT ck_movie_duration_positive CHECK (duration_minutes > 0),
    CONSTRAINT ck_movie_status CHECK (status IN ('coming_soon', 'now_showing', 'ended', 'archived'))
);
CREATE INDEX ix_movie_status_release_date_id ON movie (status, release_date DESC, id);

CREATE TABLE genre (
    id           bigint GENERATED ALWAYS AS IDENTITY,
    code         varchar(30)  NOT NULL,
    name         varchar(100) NOT NULL,
    description  text,
    is_active    boolean      NOT NULL DEFAULT true,
    CONSTRAINT pk_genre PRIMARY KEY (id),
    CONSTRAINT uq_genre_code UNIQUE (code),
    CONSTRAINT ck_genre_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_genre_name_nonblank CHECK (btrim(name) <> '')
);
CREATE UNIQUE INDEX ux_genre_name_normalized ON genre (lower(btrim(name)));

-- Bang noi N-N (PK ghep). Xoa phim (hard delete) thi go lien ket; the loai dang dung thi RESTRICT.
CREATE TABLE movie_genre (
    movie_id  bigint NOT NULL,
    genre_id  bigint NOT NULL,
    CONSTRAINT pk_movie_genre PRIMARY KEY (movie_id, genre_id),
    CONSTRAINT fk_movie_genre_movie FOREIGN KEY (movie_id) REFERENCES movie (id)
        ON UPDATE RESTRICT ON DELETE CASCADE,
    CONSTRAINT fk_movie_genre_genre FOREIGN KEY (genre_id) REFERENCES genre (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);
CREATE INDEX ix_movie_genre_genre_movie ON movie_genre (genre_id, movie_id);

-- =====================================================================
-- 2. RAP, PHONG CHIEU, GHE, LOAI VE
-- =====================================================================
CREATE TABLE cinema (
    id       bigint GENERATED ALWAYS AS IDENTITY,
    code     varchar(30)  NOT NULL,
    name     varchar(150) NOT NULL,
    address  text         NOT NULL,
    phone    varchar(30),
    status   varchar(20)  NOT NULL DEFAULT 'active',
    CONSTRAINT pk_cinema PRIMARY KEY (id),
    CONSTRAINT uq_cinema_code UNIQUE (code),
    CONSTRAINT ck_cinema_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_cinema_name_nonblank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_cinema_address_nonblank CHECK (btrim(address) <> ''),
    CONSTRAINT ck_cinema_status CHECK (status IN ('active', 'inactive'))
);

CREATE TABLE cinema_room (
    id         bigint GENERATED ALWAYS AS IDENTITY,
    cinema_id  bigint       NOT NULL,
    code       varchar(20)  NOT NULL,
    name       varchar(100) NOT NULL,
    status     varchar(20)  NOT NULL DEFAULT 'active',
    CONSTRAINT pk_cinema_room PRIMARY KEY (id),
    CONSTRAINT fk_cinema_room_cinema FOREIGN KEY (cinema_id) REFERENCES cinema (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT uq_cinema_room_cinema_code UNIQUE (cinema_id, code),
    CONSTRAINT ck_cinema_room_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_cinema_room_name_nonblank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_cinema_room_status CHECK (status IN ('active', 'maintenance', 'inactive'))
);
CREATE UNIQUE INDEX ux_cinema_room_name_normalized ON cinema_room (cinema_id, lower(btrim(name)));

CREATE TABLE seat_type (
    id                bigint GENERATED ALWAYS AS IDENTITY,
    code              varchar(30)   NOT NULL,
    name              varchar(100)  NOT NULL,
    capacity_units    smallint      NOT NULL DEFAULT 1,
    surcharge_amount  numeric(14,0) NOT NULL DEFAULT 0,
    is_active         boolean       NOT NULL DEFAULT true,
    CONSTRAINT pk_seat_type PRIMARY KEY (id),
    CONSTRAINT uq_seat_type_code UNIQUE (code),
    CONSTRAINT ck_seat_type_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_seat_type_name_nonblank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_seat_type_capacity_units CHECK (capacity_units IN (1, 2)),
    CONSTRAINT ck_seat_type_surcharge_nonneg CHECK (surcharge_amount >= 0)
);
CREATE UNIQUE INDEX ux_seat_type_name_normalized ON seat_type (lower(btrim(name)));

CREATE TABLE seat (
    id               bigint GENERATED ALWAYS AS IDENTITY,
    cinema_room_id   bigint      NOT NULL,
    seat_type_id     bigint      NOT NULL,
    row_label        varchar(10) NOT NULL,
    seat_number      smallint    NOT NULL,
    status           varchar(20) NOT NULL DEFAULT 'active',
    CONSTRAINT pk_seat PRIMARY KEY (id),
    CONSTRAINT fk_seat_cinema_room FOREIGN KEY (cinema_room_id) REFERENCES cinema_room (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_seat_seat_type FOREIGN KEY (seat_type_id) REFERENCES seat_type (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT uq_seat_position UNIQUE (cinema_room_id, row_label, seat_number),
    CONSTRAINT uq_seat_id_room UNIQUE (id, cinema_room_id),      -- dich FK ghep tu booking_detail
    CONSTRAINT ck_seat_row_label_nonblank CHECK (btrim(row_label) <> ''),
    CONSTRAINT ck_seat_number_positive CHECK (seat_number > 0),
    CONSTRAINT ck_seat_status CHECK (status IN ('active', 'maintenance', 'inactive'))
);
CREATE INDEX ix_seat_seat_type_id ON seat (seat_type_id);

CREATE TABLE ticket_type (
    id                bigint GENERATED ALWAYS AS IDENTITY,
    code              varchar(30)  NOT NULL,
    name              varchar(100) NOT NULL,
    discount_percent  numeric(5,2) NOT NULL DEFAULT 0,
    eligibility_note  text,
    is_active         boolean      NOT NULL DEFAULT true,
    CONSTRAINT pk_ticket_type PRIMARY KEY (id),
    CONSTRAINT uq_ticket_type_code UNIQUE (code),
    CONSTRAINT ck_ticket_type_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_ticket_type_name_nonblank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_ticket_type_discount_percent CHECK (discount_percent BETWEEN 0 AND 100)
);
CREATE UNIQUE INDEX ux_ticket_type_name_normalized ON ticket_type (lower(btrim(name)));

-- =====================================================================
-- 3. SUAT CHIEU
-- =====================================================================
CREATE TABLE showtime (
    id                         bigint GENERATED ALWAYS AS IDENTITY,
    movie_id                   bigint        NOT NULL,
    cinema_room_id             bigint        NOT NULL,
    movie_title_snapshot       varchar(255)  NOT NULL,
    room_name_snapshot         varchar(100)  NOT NULL,
    duration_minutes_snapshot  smallint      NOT NULL,
    starts_at                  timestamptz   NOT NULL,
    ends_at                    timestamptz   NOT NULL,
    room_available_at          timestamptz   NOT NULL,      -- gom thoi gian don phong
    booking_closes_at          timestamptz,
    base_price                 numeric(14,0) NOT NULL,
    status                     varchar(20)   NOT NULL DEFAULT 'draft',
    cancelled_at               timestamptz,
    cancellation_reason        text,
    CONSTRAINT pk_showtime PRIMARY KEY (id),
    CONSTRAINT fk_showtime_movie FOREIGN KEY (movie_id) REFERENCES movie (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_showtime_cinema_room FOREIGN KEY (cinema_room_id) REFERENCES cinema_room (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT uq_showtime_id_room UNIQUE (id, cinema_room_id),  -- dich FK ghep tu booking
    CONSTRAINT ck_showtime_title_snapshot_nonblank CHECK (btrim(movie_title_snapshot) <> ''),
    CONSTRAINT ck_showtime_room_snapshot_nonblank CHECK (btrim(room_name_snapshot) <> ''),
    CONSTRAINT ck_showtime_duration_positive CHECK (duration_minutes_snapshot > 0),
    CONSTRAINT ck_showtime_base_price_positive CHECK (base_price > 0),
    CONSTRAINT ck_showtime_time_order CHECK (starts_at < ends_at AND ends_at <= room_available_at),
    CONSTRAINT ck_showtime_booking_closes CHECK (booking_closes_at IS NULL OR booking_closes_at <= starts_at),
    CONSTRAINT ck_showtime_status CHECK (status IN ('draft', 'scheduled', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_showtime_cancel_fields CHECK (
        (status = 'cancelled'
            AND cancelled_at IS NOT NULL
            AND cancellation_reason IS NOT NULL
            AND btrim(cancellation_reason) <> '')
        OR
        (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL)
    ),
    -- Khong cho 2 suat (tru suat da huy) giao nhau [starts_at, room_available_at) trong cung phong
    CONSTRAINT ex_showtime_room_time EXCLUDE USING gist (
        cinema_room_id WITH =,
        tstzrange(starts_at, room_available_at, '[)') WITH &&
    ) WHERE (status <> 'cancelled')
);
CREATE INDEX ix_showtime_movie_starts_at ON showtime (movie_id, starts_at);
CREATE INDEX ix_showtime_room_starts_at ON showtime (cinema_room_id, starts_at);

-- =====================================================================
-- 4. TAI KHOAN & PHAN QUYEN
-- =====================================================================
CREATE TABLE app_user (
    id                 bigint GENERATED ALWAYS AS IDENTITY,
    email              varchar(254) NOT NULL,
    password_hash      text         NOT NULL,
    full_name          varchar(150) NOT NULL,
    phone              varchar(20),
    status             varchar(20)  NOT NULL DEFAULT 'active',
    email_verified_at  timestamptz,
    last_login_at      timestamptz,
    CONSTRAINT pk_app_user PRIMARY KEY (id),
    CONSTRAINT ck_app_user_email_nonblank CHECK (btrim(email) <> ''),
    CONSTRAINT ck_app_user_password_hash_nonblank CHECK (btrim(password_hash) <> ''),
    CONSTRAINT ck_app_user_full_name_nonblank CHECK (btrim(full_name) <> ''),
    CONSTRAINT ck_app_user_status CHECK (status IN ('active', 'locked', 'deactivated'))
);
CREATE UNIQUE INDEX ux_app_user_email_normalized ON app_user (lower(btrim(email)));

CREATE TABLE role (
    id           bigint GENERATED ALWAYS AS IDENTITY,
    code         varchar(30)  NOT NULL,
    name         varchar(100) NOT NULL,
    description  text,
    is_active    boolean      NOT NULL DEFAULT true,
    CONSTRAINT pk_role PRIMARY KEY (id),
    CONSTRAINT uq_role_code UNIQUE (code),
    CONSTRAINT ck_role_code_nonblank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_role_name_nonblank CHECK (btrim(name) <> '')
);

-- Bang noi N-N (PK ghep)
CREATE TABLE user_role (
    user_id              bigint      NOT NULL,
    role_id              bigint      NOT NULL,
    assigned_at          timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by_user_id  bigint,
    CONSTRAINT pk_user_role PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_user_role_user FOREIGN KEY (user_id) REFERENCES app_user (id)
        ON UPDATE RESTRICT ON DELETE CASCADE,
    CONSTRAINT fk_user_role_role FOREIGN KEY (role_id) REFERENCES role (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_user_role_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES app_user (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT
);
CREATE INDEX ix_user_role_role_user ON user_role (role_id, user_id);
CREATE INDEX ix_user_role_assigned_by ON user_role (assigned_by_user_id);

-- =====================================================================
-- 5. BOOKING & BOOKING_DETAIL
-- =====================================================================
CREATE TABLE booking (
    id                     bigint GENERATED ALWAYS AS IDENTITY,
    booking_code           uuid          NOT NULL,           -- backend sinh
    user_id                bigint        NOT NULL,
    showtime_id            bigint        NOT NULL,
    cinema_room_id         bigint        NOT NULL,           -- phong cua showtime (cho FK ghep)
    request_key            uuid          NOT NULL,           -- idempotency
    request_fingerprint    varchar(64)   NOT NULL,           -- SHA-256 du lieu dat da chuan hoa
    contact_email_snapshot varchar(254)  NOT NULL,
    contact_phone_snapshot varchar(20),
    total_amount           numeric(14,0) NOT NULL,
    currency               varchar(3)    NOT NULL DEFAULT 'VND',
    expires_at             timestamptz   NOT NULL,           -- han giu ghe (5 phut tu created_at, do service dat)
    status                 varchar(20)   NOT NULL DEFAULT 'pending',
    confirmed_at           timestamptz,
    cancelled_at           timestamptz,
    cancellation_reason    text,
    created_at             timestamptz   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_booking PRIMARY KEY (id),
    CONSTRAINT uq_booking_code UNIQUE (booking_code),
    CONSTRAINT uq_booking_user_request_key UNIQUE (user_id, request_key),
    CONSTRAINT uq_booking_id_showtime_room UNIQUE (id, showtime_id, cinema_room_id),  -- dich FK ghep tu detail
    CONSTRAINT fk_booking_user FOREIGN KEY (user_id) REFERENCES app_user (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_booking_showtime_room FOREIGN KEY (showtime_id, cinema_room_id)
        REFERENCES showtime (id, cinema_room_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT ck_booking_fingerprint_nonblank CHECK (btrim(request_fingerprint) <> ''),
    CONSTRAINT ck_booking_contact_email_nonblank CHECK (btrim(contact_email_snapshot) <> ''),
    CONSTRAINT ck_booking_total_amount_positive CHECK (total_amount > 0),
    CONSTRAINT ck_booking_currency CHECK (currency = 'VND'),
    CONSTRAINT ck_booking_expires_after_created CHECK (expires_at > created_at),
    CONSTRAINT ck_booking_status CHECK (status IN ('pending', 'confirmed', 'expired', 'cancelled')),
    CONSTRAINT ck_booking_confirmed_fields CHECK (
        (status = 'confirmed' AND confirmed_at IS NOT NULL)
        OR (status IN ('pending', 'expired') AND confirmed_at IS NULL)
        OR status = 'cancelled'
    ),
    CONSTRAINT ck_booking_cancel_fields CHECK (
        (status = 'cancelled'
            AND cancelled_at IS NOT NULL
            AND cancellation_reason IS NOT NULL
            AND btrim(cancellation_reason) <> '')
        OR
        (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL)
    )
);
CREATE INDEX ix_booking_user_history ON booking (user_id, created_at DESC, id DESC);
CREATE INDEX ix_booking_showtime_status ON booking (showtime_id, status, id);
CREATE INDEX ix_booking_expiry ON booking (expires_at, id) WHERE status = 'pending';

CREATE TABLE booking_detail (
    id                          bigint GENERATED ALWAYS AS IDENTITY,
    booking_id                  bigint        NOT NULL,
    showtime_id                 bigint        NOT NULL,
    cinema_room_id              bigint        NOT NULL,
    seat_id                     bigint        NOT NULL,
    ticket_type_id              bigint        NOT NULL,
    seat_label_snapshot         varchar(30)   NOT NULL,
    seat_type_name_snapshot     varchar(100)  NOT NULL,
    ticket_type_name_snapshot   varchar(100)  NOT NULL,
    capacity_units_snapshot     smallint      NOT NULL,
    base_amount                 numeric(14,0) NOT NULL,
    surcharge_amount            numeric(14,0) NOT NULL,
    discount_percent_snapshot   numeric(5,2)  NOT NULL,
    discount_amount             numeric(14,0) NOT NULL,
    unit_price                  numeric(14,0) NOT NULL,
    allocation_status           varchar(20)   NOT NULL DEFAULT 'held',
    released_at                 timestamptz,
    CONSTRAINT pk_booking_detail PRIMARY KEY (id),
    CONSTRAINT fk_booking_detail_booking FOREIGN KEY (booking_id, showtime_id, cinema_room_id)
        REFERENCES booking (id, showtime_id, cinema_room_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_booking_detail_seat FOREIGN KEY (seat_id, cinema_room_id)
        REFERENCES seat (id, cinema_room_id) ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_booking_detail_ticket_type FOREIGN KEY (ticket_type_id) REFERENCES ticket_type (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT uq_booking_detail_booking_seat UNIQUE (booking_id, seat_id),
    CONSTRAINT ck_booking_detail_seat_label_nonblank CHECK (btrim(seat_label_snapshot) <> ''),
    CONSTRAINT ck_booking_detail_seat_type_nonblank CHECK (btrim(seat_type_name_snapshot) <> ''),
    CONSTRAINT ck_booking_detail_ticket_type_nonblank CHECK (btrim(ticket_type_name_snapshot) <> ''),
    CONSTRAINT ck_booking_detail_capacity_units CHECK (capacity_units_snapshot IN (1, 2)),
    CONSTRAINT ck_booking_detail_base_positive CHECK (base_amount > 0),
    CONSTRAINT ck_booking_detail_surcharge_nonneg CHECK (surcharge_amount >= 0),
    CONSTRAINT ck_booking_detail_discount_percent CHECK (discount_percent_snapshot BETWEEN 0 AND 100),
    CONSTRAINT ck_booking_detail_discount_amount CHECK (discount_amount >= 0 AND discount_amount <= base_amount),
    CONSTRAINT ck_booking_detail_unit_price CHECK (
        unit_price >= 0 AND unit_price = base_amount + surcharge_amount - discount_amount
    ),
    CONSTRAINT ck_booking_detail_allocation_status CHECK (allocation_status IN ('held', 'sold', 'released')),
    CONSTRAINT ck_booking_detail_released CHECK ((allocation_status = 'released') = (released_at IS NOT NULL))
);
-- Mot ghe chi co toi da 1 detail held/sold tren moi suat chieu -> chan dat trung ghe
CREATE UNIQUE INDEX ux_booking_detail_active_seat
    ON booking_detail (showtime_id, seat_id) WHERE allocation_status IN ('held', 'sold');
CREATE INDEX ix_booking_detail_seat_room ON booking_detail (seat_id, cinema_room_id);
CREATE INDEX ix_booking_detail_ticket_type ON booking_detail (ticket_type_id);

-- =====================================================================
-- 6. VE
-- =====================================================================
CREATE TABLE ticket (
    id                    bigint GENERATED ALWAYS AS IDENTITY,
    booking_detail_id     bigint      NOT NULL,
    ticket_code           uuid        NOT NULL,          -- backend sinh
    status                varchar(20) NOT NULL DEFAULT 'valid',
    issued_at             timestamptz NOT NULL,
    used_at               timestamptz,
    validated_by_user_id  bigint,
    voided_at             timestamptz,
    void_reason           text,
    CONSTRAINT pk_ticket PRIMARY KEY (id),
    CONSTRAINT uq_ticket_booking_detail UNIQUE (booking_detail_id),   -- toi da 1 ve / detail
    CONSTRAINT uq_ticket_code UNIQUE (ticket_code),
    CONSTRAINT fk_ticket_booking_detail FOREIGN KEY (booking_detail_id) REFERENCES booking_detail (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_ticket_validated_by_user FOREIGN KEY (validated_by_user_id) REFERENCES app_user (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT ck_ticket_status CHECK (status IN ('valid', 'used', 'voided')),
    CONSTRAINT ck_ticket_status_fields CHECK (
        (status = 'valid'
            AND used_at IS NULL AND validated_by_user_id IS NULL
            AND voided_at IS NULL AND void_reason IS NULL)
        OR
        (status = 'used'
            AND used_at IS NOT NULL AND validated_by_user_id IS NOT NULL
            AND voided_at IS NULL AND void_reason IS NULL)
        OR
        (status = 'voided'
            AND voided_at IS NOT NULL
            AND void_reason IS NOT NULL AND btrim(void_reason) <> ''
            AND used_at IS NULL)
    )
);
CREATE INDEX ix_ticket_validated_by_user ON ticket (validated_by_user_id);

-- =====================================================================
-- 7. THANH TOAN
-- =====================================================================
CREATE TABLE payment (
    id                      bigint GENERATED ALWAYS AS IDENTITY,
    booking_id              bigint        NOT NULL,
    provider                varchar(30)   NOT NULL,
    merchant_request_id     uuid          NOT NULL,          -- idempotency key, backend sinh
    provider_transaction_id varchar(150),
    method                  varchar(50),
    amount                  numeric(14,0) NOT NULL,
    currency                varchar(3)    NOT NULL DEFAULT 'VND',
    received_amount         numeric(18,2),
    received_currency       varchar(3),
    status                  varchar(20)   NOT NULL DEFAULT 'created',
    applied_at              timestamptz,
    paid_at                 timestamptz,
    verified_at             timestamptz,
    reconciliation_status   varchar(20)   NOT NULL DEFAULT 'none',
    failure_code            varchar(100),
    next_reconcile_at       timestamptz   DEFAULT CURRENT_TIMESTAMP,
    created_at              timestamptz   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_payment PRIMARY KEY (id),
    CONSTRAINT uq_payment_merchant_request_id UNIQUE (merchant_request_id),
    CONSTRAINT fk_payment_booking FOREIGN KEY (booking_id) REFERENCES booking (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT ck_payment_provider_nonblank CHECK (btrim(provider) <> ''),
    CONSTRAINT ck_payment_amount_positive CHECK (amount > 0),
    CONSTRAINT ck_payment_currency CHECK (currency = 'VND'),
    CONSTRAINT ck_payment_received_amount_nonneg CHECK (received_amount IS NULL OR received_amount >= 0),
    CONSTRAINT ck_payment_status CHECK (status IN ('created', 'pending', 'unknown', 'succeeded', 'failed', 'cancelled')),
    CONSTRAINT ck_payment_reconciliation_status CHECK (reconciliation_status IN ('none', 'needs_review', 'refund_required', 'resolved')),
    CONSTRAINT ck_payment_succeeded_fields CHECK (
        status <> 'succeeded'
        OR (paid_at IS NOT NULL
            AND verified_at IS NOT NULL
            AND received_amount IS NOT NULL
            AND received_currency IS NOT NULL
            AND provider_transaction_id IS NOT NULL)
    ),
    CONSTRAINT ck_payment_applied CHECK (
        applied_at IS NULL
        OR (status = 'succeeded'
            AND received_amount = amount
            AND received_currency = currency)
    )
);
CREATE UNIQUE INDEX ux_payment_provider_transaction
    ON payment (provider, provider_transaction_id) WHERE provider_transaction_id IS NOT NULL;
CREATE UNIQUE INDEX ux_payment_open_booking
    ON payment (booking_id) WHERE status IN ('created', 'pending', 'unknown');
CREATE UNIQUE INDEX ux_payment_applied_booking
    ON payment (booking_id) WHERE applied_at IS NOT NULL;
CREATE INDEX ix_payment_booking_created ON payment (booking_id, created_at DESC);
CREATE INDEX ix_payment_reconcile_due
    ON payment (next_reconcile_at, id) WHERE status IN ('created', 'pending', 'unknown');
CREATE INDEX ix_payment_reconciliation_queue
    ON payment (reconciliation_status, created_at, id) WHERE reconciliation_status IN ('needs_review', 'refund_required');
CREATE INDEX ix_payment_paid_at ON payment (paid_at) WHERE status = 'succeeded';

CREATE TABLE payment_event (
    id               bigint GENERATED ALWAYS AS IDENTITY,
    payment_id       bigint       NOT NULL,
    source           varchar(20)  NOT NULL,
    event_key        varchar(200) NOT NULL,
    event_type       varchar(50)  NOT NULL,
    payload_summary  jsonb        NOT NULL DEFAULT '{}'::jsonb,   -- khong luu secret / du lieu the
    received_at      timestamptz  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at     timestamptz,
    status           varchar(20)  NOT NULL DEFAULT 'received',
    retry_count      integer      NOT NULL DEFAULT 0,
    next_retry_at    timestamptz  DEFAULT CURRENT_TIMESTAMP,
    last_error       varchar(500),
    CONSTRAINT pk_payment_event PRIMARY KEY (id),
    CONSTRAINT uq_payment_event_payment_key UNIQUE (payment_id, event_key),
    CONSTRAINT fk_payment_event_payment FOREIGN KEY (payment_id) REFERENCES payment (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT ck_payment_event_source CHECK (source IN ('webhook', 'reconcile')),
    CONSTRAINT ck_payment_event_key_nonblank CHECK (btrim(event_key) <> ''),
    CONSTRAINT ck_payment_event_type_nonblank CHECK (btrim(event_type) <> ''),
    CONSTRAINT ck_payment_event_status CHECK (status IN ('received', 'processed', 'failed')),
    CONSTRAINT ck_payment_event_retry_count CHECK (retry_count >= 0),
    CONSTRAINT ck_payment_event_processed CHECK ((status = 'processed') = (processed_at IS NOT NULL))
);
CREATE INDEX ix_payment_event_retry
    ON payment_event (next_retry_at, id) WHERE status IN ('received', 'failed');

-- Cau truc mo rong, khong thuoc luong MVP
CREATE TABLE refund (
    id                    bigint GENERATED ALWAYS AS IDENTITY,
    payment_id            bigint        NOT NULL,
    merchant_refund_id    uuid          NOT NULL,
    provider_refund_id    varchar(150),
    amount                numeric(18,2) NOT NULL,
    currency              varchar(3)    NOT NULL,
    reason                text          NOT NULL,
    status                varchar(20)   NOT NULL DEFAULT 'pending',
    requested_by_user_id  bigint,                          -- NULL neu do he thong
    completed_at          timestamptz,
    next_retry_at         timestamptz   DEFAULT CURRENT_TIMESTAMP,
    last_error            varchar(500),
    CONSTRAINT pk_refund PRIMARY KEY (id),
    CONSTRAINT uq_refund_payment UNIQUE (payment_id),               -- toi da 1 refund / payment
    CONSTRAINT uq_refund_merchant_refund_id UNIQUE (merchant_refund_id),
    CONSTRAINT fk_refund_payment FOREIGN KEY (payment_id) REFERENCES payment (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT fk_refund_requested_by_user FOREIGN KEY (requested_by_user_id) REFERENCES app_user (id)
        ON UPDATE RESTRICT ON DELETE RESTRICT,
    CONSTRAINT ck_refund_amount_positive CHECK (amount > 0),
    CONSTRAINT ck_refund_currency_nonblank CHECK (btrim(currency) <> ''),
    CONSTRAINT ck_refund_reason_nonblank CHECK (btrim(reason) <> ''),
    CONSTRAINT ck_refund_status CHECK (status IN ('pending', 'unknown', 'succeeded', 'failed')),
    CONSTRAINT ck_refund_completed CHECK ((status = 'succeeded') = (completed_at IS NOT NULL))
);
CREATE INDEX ix_refund_retry ON refund (next_retry_at, id) WHERE status IN ('pending', 'unknown', 'failed');
CREATE INDEX ix_refund_requested_by ON refund (requested_by_user_id);
CREATE INDEX ix_refund_completed_at ON refund (completed_at) WHERE status = 'succeeded';

-- =====================================================================
-- 8. DU LIEU KHOI TAO (seed role theo database-schema.md muc 3.11)
-- =====================================================================
INSERT INTO role (code, name) VALUES
    ('CUSTOMER', 'Customer'),
    ('STAFF',    'Staff'),
    ('MANAGER',  'Manager'),
    ('ADMIN',    'Admin')
ON CONFLICT (code) DO NOTHING;

COMMIT;
