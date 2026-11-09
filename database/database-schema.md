# Database Schema — Promethe-Phos

Bản 09/11/2026; **proposed physical baseline** cho Use-Case v2.4 / SAD v3.15. Data dictionary dưới đây được tạo từ cùng định nghĩa đã dùng sinh `database-schema.sql`, gồm **41 bảng**: 37 relation trong relational model và 4 bảng hỗ trợ Identity. Không phải snapshot database đang chạy hoặc migration 18 bảng cũ.

## 1. Cách áp dụng và quy ước

- Namespace `cinema`, bootstrap trên namespace chưa tồn tại. CREATE SCHEMA cố ý fail khi đã tồn tại; không DROP hoặc IF NOT EXISTS để che schema drift.
- PK/FK numeric dùng bigint, identity claims dùng integer theo model hỗ trợ Identity; security/public IDs dùng UUID do application sinh.
- Domain `vnd_amount`: numeric không làm tròn scale trước CHECK; giá trị nguyên, 0..99999999999999. Column NOT NULL được ghi riêng. Financial received/refund evidence dùng numeric để giữ số tiền/currency lệch.
- FK mặc định PostgreSQL NO ACTION; không cascade xóa financial/history. Một FK refresh replacement được deferred; các constraint khác kiểm immediate trừ khi SQL ghi khác.
- Cột có `NOT NULL`/`PRIMARY KEY` không nhận NULL; các cột khác nhận NULL. Thiếu DEFAULT thì application phải cung cấp khi cần.
- CHECK không xác nhận current time/role/provider evidence hoặc toàn bộ state transitions. Unique/foreign key không thay business transaction. `version` do application tăng, không tự cập nhật.
- Chuỗi hash/credential là dữ liệu đã xử lý an toàn; DDL không tự hash/encrypt. Bảng Identity cần Fluent API tương ứng, không tự dùng với mặc định AspNetUsers.
- Index no-time: không dùng now() trong partial UQ; worker/service chủ động kết thúc expired active/session/selection records.
- SQL dùng GiST native singleton room range và timestamp range, không cần extension btree_gist. Xem rationale trong database-design.md.

## 2. Danh mục bảng

| Bảng | Owner | Primary key / key shape |
|---|---|---|
| `app_user` | C03 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `role` | C03 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `user_role` | C03 | `PRIMARY KEY (user_id,role_id)` |
| `user_claim` | C03 / ASP.NET Core Identity support | `id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `role_claim` | C03 / ASP.NET Core Identity support | `id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `user_login` | C03 / ASP.NET Core Identity support | `PRIMARY KEY (login_provider,provider_key)` |
| `user_token` | C03 / ASP.NET Core Identity support | `PRIMARY KEY (user_id,login_provider,name)` |
| `authenticated_session` | C03 | `id uuid PRIMARY KEY` |
| `refresh_token` | C03 | `id uuid PRIMARY KEY` |
| `session_replacement_challenge` | C03 | `id uuid PRIMARY KEY` |
| `email_verification_request` | C03 | `id uuid PRIMARY KEY` |
| `email_address_claim` | C03 | `normalized_email varchar(256) PRIMARY KEY CHECK (btrim(normalized_email) <> '')` |
| `checkout_identity` | C08 | `id uuid PRIMARY KEY` |
| `movie` | C04 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `genre` | C04 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `movie_genre` | C04 | `PRIMARY KEY (movie_id,genre_id)` |
| `cinema` | C05 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `cinema_assignment` | C03 | `PRIMARY KEY (user_id,cinema_id,assignment_role)` |
| `cinema_room` | C05 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `seat_type` | C05 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `seat` | C05 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `ticket_category` | C07 | `code varchar(16) PRIMARY KEY CHECK (code IN ('Adult','Discounted'))` |
| `cinema_ticket_price` | C07 | `PRIMARY KEY (cinema_id,category_code)` |
| `showtime` | C06 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `showtime_change` | C06 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `selection_session` | C08 | `id uuid PRIMARY KEY` |
| `selection_seat` | C08 | `PRIMARY KEY (selection_id,seat_id)` |
| `booking` | C08 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_category` | C08 | `PRIMARY KEY (booking_id,category_code)` |
| `booking_seat` | C08 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `operation_receipt` | C13 mechanism; caller owns result | `id uuid PRIMARY KEY` |
| `payment` | C09 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `payment_event` | C09 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `ticket` | C11 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `admission_operation` | C11 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `cancellation_request` | C10 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `refund` | C10 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `refund_completion_evidence` | C10 | `refund_id bigint PRIMARY KEY REFERENCES refund(id)` |
| `notification_request` | C15 | `id uuid PRIMARY KEY` |
| `notification_attempt` | C15 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `audit_event` | C16 | `id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |

## 3. Đặc tả từng bảng

### 3.1. app_user

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `app_user`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `user_name` | `varchar(256) NOT NULL` |
| `normalized_user_name` | `varchar(256) NOT NULL UNIQUE` |
| `primary_email` | `varchar(256) NOT NULL` |
| `normalized_email` | `varchar(256) NOT NULL UNIQUE` |
| `email_confirmed` | `boolean NOT NULL DEFAULT false` |
| `password_hash` | `text` |
| `security_stamp` | `text` |
| `concurrency_stamp` | `text` |
| `phone_number` | `text` |
| `phone_number_confirmed` | `boolean NOT NULL DEFAULT false` |
| `two_factor_enabled` | `boolean NOT NULL DEFAULT false` |
| `lockout_end` | `timestamptz` |
| `lockout_enabled` | `boolean NOT NULL DEFAULT true` |
| `access_failed_count` | `integer NOT NULL DEFAULT 0 CHECK (access_failed_count >= 0)` |
| `full_name` | `varchar(150) NOT NULL CHECK (btrim(full_name) <> '')` |
| `account_state` | `varchar(20) NOT NULL DEFAULT 'active' CHECK (account_state IN ('active','locked','deactivated'))` |
| `created_at` | `timestamptz NOT NULL DEFAULT clock_timestamp()` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
CHECK (btrim(primary_email) <> '' AND btrim(normalized_email) <> '' AND btrim(normalized_user_name) <> '')
```

### 3.2. role

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `role`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `code` | `varchar(30) NOT NULL UNIQUE CHECK (code IN ('CUSTOMER','STAFF','MANAGER','ADMIN'))` |
| `name` | `varchar(256) NOT NULL` |
| `normalized_name` | `varchar(256) NOT NULL UNIQUE` |
| `concurrency_stamp` | `text` |

### 3.3. user_role

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `user_role`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `role_id` | `bigint NOT NULL REFERENCES role(id)` |
| `assigned_at` | `timestamptz NOT NULL DEFAULT clock_timestamp()` |
| `assigned_by_user_id` | `bigint REFERENCES app_user(id)` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (user_id,role_id)
```

Index bổ sung:

```sql
CREATE INDEX ix_user_role_role ON user_role(role_id,user_id);
```

### 3.4. user_claim

Bảng hỗ trợ Identity store; không thêm numbered use case.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `claim_type` | `text` |
| `claim_value` | `text` |

Index bổ sung:

```sql
CREATE INDEX ix_user_claim_owner ON user_claim(user_id);
```

### 3.5. role_claim

Bảng hỗ trợ Identity store; không thêm numbered use case.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `role_id` | `bigint NOT NULL REFERENCES role(id)` |
| `claim_type` | `text` |
| `claim_value` | `text` |

Index bổ sung:

```sql
CREATE INDEX ix_role_claim_owner ON role_claim(role_id);
```

### 3.6. user_login

Bảng hỗ trợ Identity store; không thêm numbered use case.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `login_provider` | `varchar(128) NOT NULL` |
| `provider_key` | `varchar(128) NOT NULL` |
| `provider_display_name` | `text` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (login_provider,provider_key)
```

Index bổ sung:

```sql
CREATE INDEX ix_user_login_user ON user_login(user_id);
```

### 3.7. user_token

Bảng hỗ trợ Identity store; không thêm numbered use case.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `login_provider` | `varchar(128) NOT NULL` |
| `name` | `varchar(128) NOT NULL` |
| `value` | `text` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (user_id,login_provider,name)
```

### 3.8. authenticated_session

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `authenticated_session`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `started_at` | `timestamptz NOT NULL` |
| `expires_at` | `timestamptz NOT NULL` |
| `revoked_at` | `timestamptz` |
| `revocation_reason` | `text` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,user_id),
CHECK (expires_at > started_at),
CHECK (revoked_at IS NULL OR revoked_at >= started_at)
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_session_unrevoked ON authenticated_session(user_id) WHERE revoked_at IS NULL;
```

### 3.9. refresh_token

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `refresh_token`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `session_id` | `uuid NOT NULL REFERENCES authenticated_session(id)` |
| `token_hash` | `text NOT NULL UNIQUE CHECK (btrim(token_hash) <> '')` |
| `created_at` | `timestamptz NOT NULL` |
| `expires_at` | `timestamptz NOT NULL` |
| `consumed_at` | `timestamptz` |
| `revoked_at` | `timestamptz` |
| `replacement_token_id` | `uuid` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,session_id),
FOREIGN KEY (replacement_token_id,session_id) REFERENCES refresh_token(id,session_id) DEFERRABLE INITIALLY DEFERRED,
CHECK (expires_at > created_at),
CHECK (replacement_token_id IS NULL OR (replacement_token_id <> id AND consumed_at IS NOT NULL))
```

Index bổ sung:

```sql
CREATE INDEX ix_refresh_session ON refresh_token(session_id);
```

### 3.10. session_replacement_challenge

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `session_replacement_challenge`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `expected_session_id` | `uuid NOT NULL` |
| `challenge_hash` | `text NOT NULL UNIQUE CHECK (btrim(challenge_hash) <> '')` |
| `created_at` | `timestamptz NOT NULL` |
| `expires_at` | `timestamptz NOT NULL` |
| `consumed_at` | `timestamptz` |

Ràng buộc cấp bảng:

```sql
FOREIGN KEY (expected_session_id,user_id) REFERENCES authenticated_session(id,user_id),
CHECK (expires_at > created_at)
```

### 3.11. email_verification_request

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `email_verification_request`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `purpose` | `varchar(24) NOT NULL CHECK (purpose IN ('initial_verification','email_change'))` |
| `target_email` | `varchar(256) NOT NULL CHECK (btrim(target_email) <> '')` |
| `normalized_target_email` | `varchar(256) NOT NULL CHECK (btrim(normalized_target_email) <> '')` |
| `token_hash` | `text NOT NULL UNIQUE CHECK (btrim(token_hash) <> '')` |
| `original_requested_at` | `timestamptz NOT NULL` |
| `last_sent_at` | `timestamptz NOT NULL` |
| `token_expires_at` | `timestamptz NOT NULL` |
| `reservation_expires_at` | `timestamptz` |
| `next_resend_at` | `timestamptz NOT NULL` |
| `state` | `varchar(16) NOT NULL DEFAULT 'active' CHECK (state IN ('active','verified','cancelled','expired','replaced'))` |
| `consumed_at` | `timestamptz` |
| `verified_at` | `timestamptz` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,user_id,normalized_target_email),
CHECK (last_sent_at >= original_requested_at AND token_expires_at > last_sent_at AND next_resend_at >= last_sent_at + interval '60 seconds'),
CHECK ((purpose = 'initial_verification' AND reservation_expires_at IS NULL) OR (purpose = 'email_change' AND reservation_expires_at IS NOT NULL AND reservation_expires_at = original_requested_at + interval '72 hours' AND token_expires_at <= reservation_expires_at)),
CHECK ((state = 'verified' AND consumed_at IS NOT NULL AND verified_at IS NOT NULL) OR (state <> 'verified' AND consumed_at IS NULL AND verified_at IS NULL))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_verification_active_user ON email_verification_request(user_id) WHERE state = 'active';
CREATE INDEX ix_verification_reservation_history ON email_verification_request(user_id,normalized_target_email,original_requested_at DESC);
```

### 3.12. email_address_claim

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `email_address_claim`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `normalized_email` | `varchar(256) PRIMARY KEY CHECK (btrim(normalized_email) <> '')` |
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `verification_request_id` | `uuid` |
| `claim_kind` | `varchar(16) NOT NULL CHECK (claim_kind IN ('primary','reservation'))` |
| `reserved_until` | `timestamptz` |

Ràng buộc cấp bảng:

```sql
FOREIGN KEY (verification_request_id,user_id,normalized_email) REFERENCES email_verification_request(id,user_id,normalized_target_email),
CHECK ((claim_kind = 'primary' AND verification_request_id IS NULL AND reserved_until IS NULL) OR (claim_kind = 'reservation' AND verification_request_id IS NOT NULL AND reserved_until IS NOT NULL))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_primary_claim_user ON email_address_claim(user_id) WHERE claim_kind = 'primary';
```

### 3.13. checkout_identity

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `checkout_identity`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `user_id` | `bigint REFERENCES app_user(id)` |
| `guest_credential_hash` | `text UNIQUE` |
| `guest_expires_at` | `timestamptz` |
| `revoked_at` | `timestamptz` |
| `created_at` | `timestamptz NOT NULL` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
CHECK ((user_id IS NOT NULL AND guest_credential_hash IS NULL AND guest_expires_at IS NULL) OR (user_id IS NULL AND guest_credential_hash IS NOT NULL AND btrim(guest_credential_hash) <> '' AND guest_expires_at IS NOT NULL AND guest_expires_at > created_at))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_checkout_user ON checkout_identity(user_id) WHERE user_id IS NOT NULL;
```

### 3.14. movie

Owner: C04. Logical reference: `docs/analysis and design/relational-model.md`, relation `movie`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `seed_key` | `varchar(100) UNIQUE` |
| `title` | `varchar(255) NOT NULL CHECK (btrim(title) <> '')` |
| `description` | `text` |
| `duration_minutes` | `smallint NOT NULL CHECK (duration_minutes > 0)` |
| `release_date` | `date` |
| `release_market` | `varchar(20)` |
| `poster_url` | `text` |
| `trailer_url` | `text` |
| `language` | `varchar(50)` |
| `age_rating` | `varchar(30)` |
| `age_rating_system` | `varchar(30)` |
| `status` | `varchar(20) NOT NULL DEFAULT 'coming_soon' CHECK (status IN ('coming_soon','now_showing','ended','archived'))` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Index bổ sung:

```sql
CREATE INDEX ix_movie_public_sort ON movie(release_date DESC NULLS LAST,id DESC) WHERE status IN ('coming_soon','now_showing');
```

### 3.15. genre

Owner: C04. Logical reference: `docs/analysis and design/relational-model.md`, relation `genre`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `code` | `varchar(30) NOT NULL UNIQUE CHECK (btrim(code) <> '')` |
| `name` | `varchar(100) NOT NULL CHECK (btrim(name) <> '')` |
| `normalized_name` | `varchar(100) NOT NULL UNIQUE CHECK (btrim(normalized_name) <> '')` |
| `description` | `text` |
| `is_active` | `boolean NOT NULL DEFAULT true` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

### 3.16. movie_genre

Owner: C04. Logical reference: `docs/analysis and design/relational-model.md`, relation `movie_genre`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `movie_id` | `bigint NOT NULL REFERENCES movie(id)` |
| `genre_id` | `bigint NOT NULL REFERENCES genre(id)` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (movie_id,genre_id)
```

Index bổ sung:

```sql
CREATE INDEX ix_movie_genre_reverse ON movie_genre(genre_id,movie_id);
```

### 3.17. cinema

Owner: C05. Logical reference: `docs/analysis and design/relational-model.md`, relation `cinema`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `code` | `varchar(40) NOT NULL UNIQUE CHECK (btrim(code) <> '')` |
| `name` | `varchar(150) NOT NULL CHECK (btrim(name) <> '')` |
| `address` | `text NOT NULL CHECK (btrim(address) <> '')` |
| `phone` | `varchar(30)` |
| `timezone` | `varchar(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh'` |
| `status` | `varchar(16) NOT NULL DEFAULT 'active' CHECK (status IN ('active','inactive'))` |
| `minimum_turnaround_minutes` | `smallint NOT NULL DEFAULT 15 CHECK (minimum_turnaround_minutes >= 0)` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

### 3.18. cinema_assignment

Owner: C03. Logical reference: `docs/analysis and design/relational-model.md`, relation `cinema_assignment`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `cinema_id` | `bigint NOT NULL REFERENCES cinema(id)` |
| `assignment_role` | `varchar(16) NOT NULL CHECK (assignment_role IN ('STAFF','MANAGER'))` |
| `assigned_at` | `timestamptz NOT NULL DEFAULT clock_timestamp()` |
| `assigned_by_user_id` | `bigint NOT NULL REFERENCES app_user(id)` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (user_id,cinema_id,assignment_role)
```

Index bổ sung:

```sql
CREATE INDEX ix_assignment_cinema ON cinema_assignment(cinema_id,assignment_role,user_id);
```

### 3.19. cinema_room

Owner: C05. Logical reference: `docs/analysis and design/relational-model.md`, relation `cinema_room`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `cinema_id` | `bigint NOT NULL REFERENCES cinema(id)` |
| `code` | `varchar(30) NOT NULL CHECK (btrim(code) <> '')` |
| `name` | `varchar(100) NOT NULL CHECK (btrim(name) <> '')` |
| `status` | `varchar(16) NOT NULL DEFAULT 'active' CHECK (status IN ('active','inactive','maintenance'))` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (cinema_id,code),
UNIQUE (id,cinema_id)
```

### 3.20. seat_type

Owner: C05. Logical reference: `docs/analysis and design/relational-model.md`, relation `seat_type`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `code` | `varchar(30) NOT NULL UNIQUE CHECK (btrim(code) <> '')` |
| `name` | `varchar(100) NOT NULL CHECK (btrim(name) <> '')` |
| `surcharge_amount` | `vnd_amount NOT NULL DEFAULT 0` |
| `is_active` | `boolean NOT NULL DEFAULT true` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

### 3.21. seat

Owner: C05. Logical reference: `docs/analysis and design/relational-model.md`, relation `seat`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `cinema_room_id` | `bigint NOT NULL REFERENCES cinema_room(id)` |
| `seat_type_id` | `bigint NOT NULL REFERENCES seat_type(id)` |
| `row_label` | `varchar(10) NOT NULL CHECK (btrim(row_label) <> '')` |
| `seat_number` | `smallint NOT NULL CHECK (seat_number > 0)` |
| `x_position` | `integer NOT NULL CHECK (x_position >= 0)` |
| `y_position` | `integer NOT NULL CHECK (y_position >= 0)` |
| `operational_state` | `varchar(20) NOT NULL DEFAULT 'active' CHECK (operational_state IN ('active','out_of_service'))` |
| `retired_at` | `timestamptz` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (cinema_room_id,row_label,seat_number),
UNIQUE (cinema_room_id,x_position,y_position),
UNIQUE (id,cinema_room_id)
```

### 3.22. ticket_category

Owner: C07. Logical reference: `docs/analysis and design/relational-model.md`, relation `ticket_category`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `code` | `varchar(16) PRIMARY KEY CHECK (code IN ('Adult','Discounted'))` |
| `display_name` | `varchar(100) NOT NULL CHECK (btrim(display_name) <> '')` |
| `eligibility_description` | `text NOT NULL` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

### 3.23. cinema_ticket_price

Owner: C07. Logical reference: `docs/analysis and design/relational-model.md`, relation `cinema_ticket_price`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `cinema_id` | `bigint NOT NULL REFERENCES cinema(id)` |
| `category_code` | `varchar(16) NOT NULL REFERENCES ticket_category(code)` |
| `amount` | `vnd_amount NOT NULL` |
| `currency` | `varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND')` |
| `updated_at` | `timestamptz NOT NULL DEFAULT clock_timestamp()` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (cinema_id,category_code)
```

### 3.24. showtime

Owner: C06. Logical reference: `docs/analysis and design/relational-model.md`, relation `showtime`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `seed_key` | `varchar(120) UNIQUE` |
| `movie_id` | `bigint NOT NULL REFERENCES movie(id)` |
| `cinema_room_id` | `bigint NOT NULL REFERENCES cinema_room(id)` |
| `duration_minutes_snapshot` | `smallint NOT NULL CHECK (duration_minutes_snapshot > 0)` |
| `turnaround_minutes_applied` | `smallint NOT NULL CHECK (turnaround_minutes_applied >= 0)` |
| `starts_at` | `timestamptz NOT NULL` |
| `ends_at` | `timestamptz NOT NULL` |
| `room_available_at` | `timestamptz NOT NULL` |
| `status` | `varchar(16) NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','scheduled','in_progress','completed','cancelled'))` |
| `cancelled_at` | `timestamptz` |
| `cancellation_reason` | `text` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,cinema_room_id),
CHECK (ends_at = starts_at + duration_minutes_snapshot * interval '1 minute'),
CHECK (room_available_at = ends_at + turnaround_minutes_applied * interval '1 minute'),
CHECK ((status = 'cancelled' AND cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '') OR (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL)),
CONSTRAINT ex_showtime_room_time EXCLUDE USING gist (int8range(cinema_room_id,cinema_room_id,'[]') WITH &&, tstzrange(starts_at,room_available_at,'[)') WITH &&) WHERE (status <> 'cancelled')
```

Index bổ sung:

```sql
CREATE INDEX ix_showtime_movie_time ON showtime(movie_id,starts_at,id);
CREATE INDEX ix_showtime_room_time ON showtime(cinema_room_id,starts_at,id);
```

### 3.25. showtime_change

Owner: C06. Logical reference: `docs/analysis and design/relational-model.md`, relation `showtime_change`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `showtime_id` | `bigint NOT NULL REFERENCES showtime(id)` |
| `old_starts_at` | `timestamptz NOT NULL` |
| `new_starts_at` | `timestamptz NOT NULL` |
| `changed_by_user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `changed_at` | `timestamptz NOT NULL` |
| `reason` | `text NOT NULL CHECK (btrim(reason) <> '')` |
| `operation_key` | `uuid NOT NULL` |

Ràng buộc cấp bảng:

```sql
UNIQUE (showtime_id,operation_key),
CHECK (old_starts_at <> new_starts_at)
```

### 3.26. selection_session

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `selection_session`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `checkout_identity_id` | `uuid NOT NULL REFERENCES checkout_identity(id)` |
| `showtime_id` | `bigint NOT NULL` |
| `cinema_room_id` | `bigint NOT NULL` |
| `started_at` | `timestamptz NOT NULL` |
| `duration_seconds` | `integer NOT NULL DEFAULT 300 CHECK (duration_seconds = 300)` |
| `expires_at` | `timestamptz NOT NULL` |
| `adult_quantity` | `integer NOT NULL CHECK (adult_quantity >= 0)` |
| `discounted_quantity` | `integer NOT NULL CHECK (discounted_quantity >= 0)` |
| `maximum_tickets_snapshot` | `integer NOT NULL DEFAULT 8 CHECK (maximum_tickets_snapshot > 0)` |
| `state` | `varchar(16) NOT NULL DEFAULT 'active' CHECK (state IN ('active','consumed','expired','abandoned'))` |
| `accepted_quote_fingerprint` | `text` |
| `quoted_adult_price` | `vnd_amount` |
| `quoted_discounted_price` | `vnd_amount` |
| `quoted_total` | `vnd_amount` |
| `quoted_showtime_start` | `timestamptz` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
FOREIGN KEY (showtime_id,cinema_room_id) REFERENCES showtime(id,cinema_room_id),
UNIQUE (id,cinema_room_id),
UNIQUE (id,checkout_identity_id,showtime_id,cinema_room_id),
CHECK (expires_at = started_at + duration_seconds * interval '1 second'),
CHECK (adult_quantity::bigint + discounted_quantity BETWEEN 1 AND maximum_tickets_snapshot)
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_selection_active ON selection_session(checkout_identity_id,showtime_id) WHERE state = 'active';
```

### 3.27. selection_seat

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `selection_seat`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `selection_id` | `uuid NOT NULL` |
| `seat_id` | `bigint NOT NULL` |
| `cinema_room_id` | `bigint NOT NULL` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (selection_id,seat_id),
FOREIGN KEY (selection_id,cinema_room_id) REFERENCES selection_session(id,cinema_room_id),
FOREIGN KEY (seat_id,cinema_room_id) REFERENCES seat(id,cinema_room_id)
```

### 3.28. booking

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `booking`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_code` | `uuid NOT NULL UNIQUE` |
| `checkout_identity_id` | `uuid NOT NULL REFERENCES checkout_identity(id)` |
| `selection_session_id` | `uuid NOT NULL UNIQUE` |
| `showtime_id` | `bigint NOT NULL` |
| `cinema_room_id` | `bigint NOT NULL` |
| `contact_full_name` | `varchar(150) NOT NULL CHECK (btrim(contact_full_name) <> '')` |
| `contact_phone` | `varchar(30) NOT NULL CHECK (btrim(contact_phone) <> '')` |
| `contact_email` | `varchar(256) NOT NULL CHECK (btrim(contact_email) <> '')` |
| `terms_accepted_at` | `timestamptz NOT NULL` |
| `age_acknowledged_at` | `timestamptz NOT NULL` |
| `purchased_start_at` | `timestamptz NOT NULL` |
| `movie_title_snapshot` | `varchar(255) NOT NULL CHECK (btrim(movie_title_snapshot) <> '')` |
| `room_name_snapshot` | `varchar(100) NOT NULL CHECK (btrim(room_name_snapshot) <> '')` |
| `reservation_expires_at` | `timestamptz NOT NULL` |
| `maximum_tickets_snapshot` | `integer NOT NULL DEFAULT 8 CHECK (maximum_tickets_snapshot > 0)` |
| `total_amount` | `vnd_amount NOT NULL` |
| `currency` | `varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND')` |
| `status` | `varchar(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','confirmed','expired','cancelled'))` |
| `created_at` | `timestamptz NOT NULL` |
| `confirmed_at` | `timestamptz` |
| `cancelled_at` | `timestamptz` |
| `cancellation_reason` | `text` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,showtime_id,cinema_room_id),
UNIQUE (id,checkout_identity_id),
FOREIGN KEY (showtime_id,cinema_room_id) REFERENCES showtime(id,cinema_room_id),
FOREIGN KEY (selection_session_id,checkout_identity_id,showtime_id,cinema_room_id) REFERENCES selection_session(id,checkout_identity_id,showtime_id,cinema_room_id),
CHECK (reservation_expires_at > created_at),
CHECK ((status = 'confirmed' AND confirmed_at IS NOT NULL) OR (status IN ('pending','expired') AND confirmed_at IS NULL) OR status = 'cancelled'),
CHECK ((status = 'cancelled' AND cancelled_at IS NOT NULL AND cancellation_reason IS NOT NULL AND btrim(cancellation_reason) <> '') OR (status <> 'cancelled' AND cancelled_at IS NULL AND cancellation_reason IS NULL))
```

Index bổ sung:

```sql
CREATE INDEX ix_booking_owner_history ON booking(checkout_identity_id,created_at DESC,id DESC);
CREATE INDEX ix_booking_showtime_state ON booking(showtime_id,status,id);
CREATE INDEX ix_booking_pending_expiry ON booking(reservation_expires_at,id) WHERE status = 'pending';
```

### 3.29. booking_category

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `booking_category`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `booking_id` | `bigint NOT NULL REFERENCES booking(id)` |
| `category_code` | `varchar(16) NOT NULL REFERENCES ticket_category(code)` |
| `quantity` | `integer NOT NULL CHECK (quantity >= 0)` |
| `base_price_snapshot` | `vnd_amount NOT NULL` |
| `category_name_snapshot` | `varchar(100) NOT NULL CHECK (btrim(category_name_snapshot) <> '')` |
| `admitted_quantity` | `integer NOT NULL DEFAULT 0` |

Ràng buộc cấp bảng:

```sql
PRIMARY KEY (booking_id,category_code),
CHECK (admitted_quantity BETWEEN 0 AND quantity)
```

### 3.30. booking_seat

Owner: C08. Logical reference: `docs/analysis and design/relational-model.md`, relation `booking_seat`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_id` | `bigint NOT NULL` |
| `showtime_id` | `bigint NOT NULL` |
| `cinema_room_id` | `bigint NOT NULL` |
| `seat_id` | `bigint NOT NULL` |
| `seat_label_snapshot` | `varchar(30) NOT NULL CHECK (btrim(seat_label_snapshot) <> '')` |
| `seat_type_name_snapshot` | `varchar(100) NOT NULL CHECK (btrim(seat_type_name_snapshot) <> '')` |
| `surcharge_snapshot` | `vnd_amount NOT NULL` |
| `allocation_status` | `varchar(16) NOT NULL DEFAULT 'held' CHECK (allocation_status IN ('held','sold','released'))` |
| `released_at` | `timestamptz` |

Ràng buộc cấp bảng:

```sql
UNIQUE (booking_id,seat_id),
FOREIGN KEY (booking_id,showtime_id,cinema_room_id) REFERENCES booking(id,showtime_id,cinema_room_id),
FOREIGN KEY (seat_id,cinema_room_id) REFERENCES seat(id,cinema_room_id),
CHECK ((allocation_status = 'released') = (released_at IS NOT NULL))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_active_seat ON booking_seat(showtime_id,seat_id) WHERE allocation_status IN ('held','sold');
```

### 3.31. operation_receipt

Owner: C13 mechanism; caller owns result. Logical reference: `docs/analysis and design/relational-model.md`, relation `operation_receipt`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `actor_scope` | `text NOT NULL CHECK (btrim(actor_scope) <> '')` |
| `operation_name` | `varchar(80) NOT NULL CHECK (btrim(operation_name) <> '')` |
| `target_scope` | `text NOT NULL CHECK (btrim(target_scope) <> '')` |
| `request_key` | `uuid NOT NULL` |
| `request_fingerprint` | `text NOT NULL CHECK (btrim(request_fingerprint) <> '')` |
| `processing_state` | `varchar(16) NOT NULL CHECK (processing_state IN ('processing','committed','rejected'))` |
| `result_reference` | `text` |
| `created_at` | `timestamptz NOT NULL` |
| `committed_at` | `timestamptz` |
| `lease_until` | `timestamptz` |
| `claim_token` | `uuid` |

Ràng buộc cấp bảng:

```sql
UNIQUE (actor_scope,operation_name,target_scope,request_key),
CHECK ((processing_state = 'committed') = (committed_at IS NOT NULL)),
CHECK ((lease_until IS NULL) = (claim_token IS NULL))
```

### 3.32. payment

Owner: C09. Logical reference: `docs/analysis and design/relational-model.md`, relation `payment`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_id` | `bigint NOT NULL REFERENCES booking(id)` |
| `provider` | `varchar(30) NOT NULL CHECK (btrim(provider) <> '')` |
| `merchant_request_id` | `uuid NOT NULL UNIQUE` |
| `provider_transaction_id` | `text` |
| `amount` | `vnd_amount NOT NULL CHECK (amount > 0)` |
| `currency` | `varchar(3) NOT NULL DEFAULT 'VND' CHECK (currency = 'VND')` |
| `received_amount` | `numeric` |
| `received_currency` | `varchar(12)` |
| `status` | `varchar(16) NOT NULL DEFAULT 'created' CHECK (status IN ('created','pending','unknown','succeeded','failed','cancelled'))` |
| `created_at` | `timestamptz NOT NULL` |
| `initiation_committed_at` | `timestamptz NOT NULL` |
| `initiated_at` | `timestamptz` |
| `payment_window_seconds` | `integer NOT NULL DEFAULT 600 CHECK (payment_window_seconds = 600)` |
| `payment_deadline` | `timestamptz` |
| `grace_seconds` | `integer NOT NULL DEFAULT 10 CHECK (grace_seconds >= 0)` |
| `grace_until` | `timestamptz` |
| `verified_at` | `timestamptz` |
| `paid_at` | `timestamptz` |
| `applied_at` | `timestamptz` |
| `reconciliation_status` | `varchar(24) NOT NULL DEFAULT 'none' CHECK (reconciliation_status IN ('none','needs_review','refund_required','resolved'))` |
| `next_reconcile_at` | `timestamptz` |
| `retry_count` | `integer NOT NULL DEFAULT 0 CHECK (retry_count >= 0)` |
| `lease_until` | `timestamptz` |
| `claim_token` | `uuid` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,booking_id),
UNIQUE (id,provider),
CHECK (provider_transaction_id IS NULL OR btrim(provider_transaction_id) <> ''),
CHECK (received_amount IS NULL OR (received_amount >= 0 AND received_amount < 1e18)),
CHECK ((initiated_at IS NULL AND payment_deadline IS NULL AND grace_until IS NULL) OR (initiated_at IS NOT NULL AND payment_deadline IS NOT NULL AND grace_until IS NOT NULL AND payment_deadline = initiated_at + payment_window_seconds * interval '1 second' AND grace_until = payment_deadline + grace_seconds * interval '1 second')),
CHECK (status <> 'succeeded' OR (verified_at IS NOT NULL AND paid_at IS NOT NULL AND received_amount IS NOT NULL AND received_currency IS NOT NULL AND provider_transaction_id IS NOT NULL)),
CHECK (applied_at IS NULL OR (status = 'succeeded' AND received_amount = amount AND received_currency = currency)),
CHECK ((lease_until IS NULL) = (claim_token IS NULL))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_payment_open ON payment(booking_id) WHERE status IN ('created','pending','unknown');
CREATE UNIQUE INDEX ux_payment_applied ON payment(booking_id) WHERE applied_at IS NOT NULL;
CREATE UNIQUE INDEX ux_payment_provider_tx ON payment(provider,provider_transaction_id) WHERE provider_transaction_id IS NOT NULL;
CREATE INDEX ix_payment_reconcile ON payment(next_reconcile_at,id) WHERE status IN ('created','pending','unknown') OR reconciliation_status IN ('needs_review','refund_required');
```

### 3.33. payment_event

Owner: C09. Logical reference: `docs/analysis and design/relational-model.md`, relation `payment_event`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `payment_id` | `bigint` |
| `provider` | `varchar(30) NOT NULL CHECK (btrim(provider) <> '')` |
| `provider_event_identity` | `text NOT NULL CHECK (btrim(provider_event_identity) <> '')` |
| `source` | `varchar(16) NOT NULL CHECK (source IN ('webhook','query'))` |
| `sanitized_payload` | `jsonb NOT NULL` |
| `received_at` | `timestamptz NOT NULL` |
| `verified_at` | `timestamptz NOT NULL` |
| `processing_status` | `varchar(16) NOT NULL DEFAULT 'received' CHECK (processing_status IN ('received','processed','failed','needs_review'))` |
| `processed_at` | `timestamptz` |
| `retry_count` | `integer NOT NULL DEFAULT 0 CHECK (retry_count >= 0)` |
| `next_retry_at` | `timestamptz` |
| `lease_until` | `timestamptz` |
| `claim_token` | `uuid` |
| `last_error_code` | `varchar(100)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (provider,provider_event_identity),
FOREIGN KEY (payment_id,provider) REFERENCES payment(id,provider),
CHECK ((processing_status = 'processed') = (processed_at IS NOT NULL)),
CHECK ((lease_until IS NULL) = (claim_token IS NULL))
```

Index bổ sung:

```sql
CREATE INDEX ix_event_due ON payment_event(next_retry_at,id) WHERE processing_status IN ('received','failed');
```

### 3.34. ticket

Owner: C11. Logical reference: `docs/analysis and design/relational-model.md`, relation `ticket`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_id` | `bigint NOT NULL UNIQUE REFERENCES booking(id)` |
| `ticket_code` | `uuid NOT NULL UNIQUE` |
| `qr_lookup_hash` | `text NOT NULL UNIQUE CHECK (btrim(qr_lookup_hash) <> '')` |
| `encrypted_qr_credential` | `bytea NOT NULL CHECK (octet_length(encrypted_qr_credential) > 0)` |
| `qr_key_version` | `varchar(60) NOT NULL CHECK (btrim(qr_key_version) <> '')` |
| `issued_at` | `timestamptz NOT NULL` |
| `status` | `varchar(16) NOT NULL DEFAULT 'valid' CHECK (status IN ('valid','voided'))` |
| `voided_at` | `timestamptz` |
| `void_reason` | `text` |

Ràng buộc cấp bảng:

```sql
UNIQUE (id,booking_id),
CHECK ((status = 'voided' AND voided_at IS NOT NULL AND void_reason IS NOT NULL AND btrim(void_reason) <> '') OR (status = 'valid' AND voided_at IS NULL AND void_reason IS NULL))
```

### 3.35. admission_operation

Owner: C11. Logical reference: `docs/analysis and design/relational-model.md`, relation `admission_operation`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_id` | `bigint NOT NULL REFERENCES booking(id)` |
| `ticket_id` | `bigint NOT NULL` |
| `staff_user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `request_key` | `uuid NOT NULL` |
| `fingerprint` | `text NOT NULL CHECK (btrim(fingerprint) <> '')` |
| `adult_quantity` | `integer NOT NULL CHECK (adult_quantity >= 0)` |
| `discounted_quantity` | `integer NOT NULL CHECK (discounted_quantity >= 0)` |
| `age_verified` | `boolean NOT NULL CHECK (age_verified)` |
| `discounted_eligibility_verified` | `boolean NOT NULL` |
| `official_start_at_validation` | `timestamptz NOT NULL` |
| `admitted_at` | `timestamptz NOT NULL` |

Ràng buộc cấp bảng:

```sql
UNIQUE (booking_id,request_key),
FOREIGN KEY (ticket_id,booking_id) REFERENCES ticket(id,booking_id),
CHECK (adult_quantity::bigint + discounted_quantity > 0),
CHECK (discounted_quantity = 0 OR discounted_eligibility_verified),
CHECK (admitted_at BETWEEN official_start_at_validation - interval '5 minutes' AND official_start_at_validation + interval '20 minutes')
```

### 3.36. cancellation_request

Owner: C10. Logical reference: `docs/analysis and design/relational-model.md`, relation `cancellation_request`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `booking_id` | `bigint NOT NULL` |
| `requested_by_identity_id` | `uuid NOT NULL` |
| `request_key` | `uuid NOT NULL` |
| `requested_at` | `timestamptz NOT NULL` |
| `purchased_start_snapshot` | `timestamptz NOT NULL` |
| `official_start_at_request` | `timestamptz NOT NULL` |
| `reason` | `text NOT NULL CHECK (btrim(reason) <> '')` |
| `status` | `varchar(32) NOT NULL DEFAULT 'pending_review' CHECK (status IN ('pending_review','approved_awaiting_refund','rejected','withdrawn','completed'))` |
| `reviewed_by_user_id` | `bigint REFERENCES app_user(id)` |
| `reviewed_at` | `timestamptz` |
| `decision_reason` | `text` |
| `withdrawn_at` | `timestamptz` |
| `completed_at` | `timestamptz` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (requested_by_identity_id,request_key),
UNIQUE (id,booking_id),
FOREIGN KEY (booking_id,requested_by_identity_id) REFERENCES booking(id,checkout_identity_id),
CHECK (purchased_start_snapshot <> official_start_at_request AND requested_at < official_start_at_request),
CHECK ((status = 'withdrawn') = (withdrawn_at IS NOT NULL)),
CHECK ((status = 'completed') = (completed_at IS NOT NULL)),
CHECK (status NOT IN ('approved_awaiting_refund','completed') OR (reviewed_at IS NOT NULL AND reviewed_by_user_id IS NOT NULL)),
CHECK (status <> 'rejected' OR (reviewed_at IS NOT NULL AND decision_reason IS NOT NULL AND btrim(decision_reason) <> ''))
```

Index bổ sung:

```sql
CREATE UNIQUE INDEX ux_cancellation_open ON cancellation_request(booking_id) WHERE status IN ('pending_review','approved_awaiting_refund');
```

### 3.37. refund

Owner: C10. Logical reference: `docs/analysis and design/relational-model.md`, relation `refund`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `payment_id` | `bigint NOT NULL UNIQUE` |
| `booking_id` | `bigint NOT NULL REFERENCES booking(id)` |
| `cancellation_request_id` | `bigint UNIQUE` |
| `origin` | `varchar(30) NOT NULL CHECK (origin IN ('payment_reconciliation','showtime_rescheduling'))` |
| `merchant_refund_id` | `uuid NOT NULL UNIQUE` |
| `amount` | `numeric NOT NULL CHECK (amount > 0 AND amount < 1e18)` |
| `currency` | `varchar(12) NOT NULL CHECK (btrim(currency) <> '')` |
| `status` | `varchar(16) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','unknown','failed','succeeded'))` |
| `created_at` | `timestamptz NOT NULL` |
| `completed_at` | `timestamptz` |
| `version` | `bigint NOT NULL DEFAULT 1 CHECK (version > 0)` |

Ràng buộc cấp bảng:

```sql
FOREIGN KEY (payment_id,booking_id) REFERENCES payment(id,booking_id),
FOREIGN KEY (cancellation_request_id,booking_id) REFERENCES cancellation_request(id,booking_id),
CHECK ((origin = 'showtime_rescheduling' AND cancellation_request_id IS NOT NULL) OR (origin = 'payment_reconciliation' AND cancellation_request_id IS NULL)),
CHECK ((status = 'succeeded') = (completed_at IS NOT NULL))
```

### 3.38. refund_completion_evidence

Owner: C10. Logical reference: `docs/analysis and design/relational-model.md`, relation `refund_completion_evidence`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `refund_id` | `bigint PRIMARY KEY REFERENCES refund(id)` |
| `transfer_channel` | `varchar(80) NOT NULL CHECK (btrim(transfer_channel) <> '')` |
| `external_reference` | `text NOT NULL CHECK (btrim(external_reference) <> '')` |
| `amount` | `numeric NOT NULL CHECK (amount > 0 AND amount < 1e18)` |
| `currency` | `varchar(12) NOT NULL CHECK (btrim(currency) <> '')` |
| `externally_completed_at` | `timestamptz NOT NULL` |
| `evidence_note` | `text NOT NULL CHECK (btrim(evidence_note) <> '')` |
| `recorded_by_user_id` | `bigint NOT NULL REFERENCES app_user(id)` |
| `recorded_at` | `timestamptz NOT NULL` |
| `operation_key` | `uuid NOT NULL UNIQUE` |

Ràng buộc cấp bảng:

```sql
UNIQUE (transfer_channel,external_reference),
CHECK (recorded_at >= externally_completed_at)
```

### 3.39. notification_request

Owner: C15. Logical reference: `docs/analysis and design/relational-model.md`, relation `notification_request`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `uuid PRIMARY KEY` |
| `deduplication_key` | `text NOT NULL UNIQUE CHECK (btrim(deduplication_key) <> '')` |
| `notification_kind` | `varchar(30) NOT NULL CHECK (notification_kind IN ('ticket_issued','ticket_resend','verification','email_change_security','showtime_changed'))` |
| `booking_id` | `bigint REFERENCES booking(id)` |
| `verification_request_id` | `uuid REFERENCES email_verification_request(id)` |
| `recipient_email` | `varchar(256) NOT NULL CHECK (btrim(recipient_email) <> '')` |
| `template_key` | `varchar(100) NOT NULL CHECK (btrim(template_key) <> '')` |
| `protected_payload` | `bytea` |
| `payload_key_version` | `varchar(60)` |
| `state` | `varchar(16) NOT NULL DEFAULT 'pending' CHECK (state IN ('pending','sending','sent','failed','cancelled'))` |
| `created_at` | `timestamptz NOT NULL` |
| `next_attempt_at` | `timestamptz` |
| `lease_until` | `timestamptz` |
| `claim_token` | `uuid` |
| `sent_at` | `timestamptz` |
| `attempt_count` | `integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0)` |

Ràng buộc cấp bảng:

```sql
CHECK ((notification_kind IN ('ticket_issued','ticket_resend','showtime_changed') AND booking_id IS NOT NULL AND verification_request_id IS NULL) OR (notification_kind IN ('verification','email_change_security') AND booking_id IS NULL AND verification_request_id IS NOT NULL)),
CHECK ((protected_payload IS NULL) = (payload_key_version IS NULL)),
CHECK ((lease_until IS NULL) = (claim_token IS NULL)),
CHECK ((state = 'sent') = (sent_at IS NOT NULL))
```

Index bổ sung:

```sql
CREATE INDEX ix_notification_due ON notification_request(next_attempt_at,id) WHERE state IN ('pending','sending','failed');
```

### 3.40. notification_attempt

Owner: C15. Logical reference: `docs/analysis and design/relational-model.md`, relation `notification_attempt`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `request_id` | `uuid NOT NULL REFERENCES notification_request(id)` |
| `attempt_no` | `integer NOT NULL CHECK (attempt_no > 0)` |
| `claim_token` | `uuid NOT NULL` |
| `started_at` | `timestamptz NOT NULL` |
| `finished_at` | `timestamptz` |
| `provider_message_id` | `text` |
| `outcome` | `varchar(16) NOT NULL CHECK (outcome IN ('started','sent','failed','unknown'))` |
| `sanitized_error_code` | `varchar(100)` |

Ràng buộc cấp bảng:

```sql
UNIQUE (request_id,attempt_no),
CHECK ((outcome = 'started' AND finished_at IS NULL) OR (outcome <> 'started' AND finished_at IS NOT NULL))
```

### 3.41. audit_event

Owner: C16. Logical reference: `docs/analysis and design/relational-model.md`, relation `audit_event`.

| Cột | Kiểu, nullability, default và constraint tại cột (SQL chính xác) |
|---|---|
| `id` | `bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY` |
| `operation_identity` | `uuid NOT NULL` |
| `event_type` | `varchar(100) NOT NULL CHECK (btrim(event_type) <> '')` |
| `actor_kind` | `varchar(16) NOT NULL CHECK (actor_kind IN ('user','guest','system','provider'))` |
| `actor_user_id` | `bigint REFERENCES app_user(id)` |
| `actor_scope` | `text` |
| `resource_type` | `varchar(80) NOT NULL CHECK (btrim(resource_type) <> '')` |
| `resource_id` | `text NOT NULL CHECK (btrim(resource_id) <> '')` |
| `occurred_at` | `timestamptz NOT NULL` |
| `correlation_id` | `text` |
| `sanitized_context` | `jsonb NOT NULL DEFAULT '{}'::jsonb` |

Ràng buộc cấp bảng:

```sql
UNIQUE (operation_identity,event_type,resource_type,resource_id),
CHECK ((actor_kind = 'user') = (actor_user_id IS NOT NULL))
```

Index bổ sung:

```sql
CREATE INDEX ix_audit_resource ON audit_event(resource_type,resource_id,occurred_at);
```

## 4. Quy tắc bổ sung ngoài CREATE TABLE

Bootstrap insert đúng Adult/Discounted. Trigger `tr_category_no_delete` ngăn DELETE category; CHECK code và PK cấm category thứ ba/duplicate. Runtime role không có TRUNCATE/DDL. Append-only triggers cấm UPDATE/DELETE trên `admission_operation`, `refund_completion_evidence`, `audit_event`, `showtime_change`.

DDL revoke PUBLIC trên schema/tables/sequences/functions; chưa tạo runtime DB role/grants hoặc RLS. Không expose schema cho browser/Supabase anon. Migration owner có thể thay cấu trúc nên không dùng làm application runtime credential.

## 5. Logical → physical bổ sung / khác biệt có chủ đích

| Nội dung | Quyết định vật lý |
|---|---|
| Identity 3 logical relations | Giữ user/role/membership; bổ sung 4 bảng claims/logins/tokens, map tất cả bằng EF |
| Email initial vs change | reservation_expires_at NULL cho initial verification; primary ownership không hết hạn vì token |
| Selection-room consistency | Thêm cinema_room_id và composite FKs vào selection_session/selection_seat |
| Selection deadline | Snapshot duration_seconds=300; không created_at+5 phút ở booking |
| Payment initiation uncertainty | Thêm initiation_committed_at; valid initiated_at/deadline có thể chưa biết |
| Guest access | Checkout identity XOR user/guest hash; expiry không xóa paid booking |
| Required ticket categories | Seed trong DDL, không phụ thuộc demo fixture |
| QR redelivery | Hash lookup + ciphertext/key version; key không ở DB |
| Seat retirement | retired_at tách operational state; không xóa/tái sử dụng ID |
| Seed identity | Nullable seed_key unique cho movie/showtime; không unique title |
| Film classification | age_rating_system/release_market ghi provenance, không đổi PG-13 thành luật admission Việt Nam |
| Schedule exclusion | Native range GiST; tất cả state trừ cancelled tham gia |
| Partial UQ key location | Booking UQ(id,showtime_id,cinema_room_id); child booking_seat cho nhiều seat |

## 6. State vocabularies và giới hạn

Enums CHECK trong từng bảng là nguồn chính xác của DDL. Cancellation request có `pending_review`, `approved_awaiting_refund`, `rejected`, `withdrawn`, `completed`; refund có `pending`, `unknown`, `failed`, `succeeded`. Payment reconciliation status riêng, không dùng `refund_required` làm financial refund state. Ticket chỉ `valid/voided`; exhausted/remaining derive từ category admissions, không one-scan used.

`movie.status=now_showing` ở seed là demo classification. `available` là seat projection từ operational state và absence of active allocation, không state lưu ở seat. Cinema/room/support-process enum choices là physical mapping proposal; thay bằng migration có review nếu implementation chọn mapping khác.

## 7. Các invariant không do DDL này bảo đảm đầy đủ

- Category rows đủ hai, quantities = số booking seats, giá tổng bằng các snapshots; reservation deadline bằng selection deadline; snapshots bất biến.
- Account/claims cùng primary email, current roles/assignments, last active Admin, permanent Customer membership, challenge/refresh consumption protocol.
- Current showtime eligibility, no structural edit dưới freeze, safe cancel/reschedule và đúng turnaround config hiện hành.
- Verified provider truth; eligible payment applied đồng bộ booking/seats/ticket/outbox/audit; no downgrade.
- Admission current authority và category ledger/counter consistency, first-admission auto-reject request, approved-cancellation restriction.
- Full refund amount/currency match, evidence verification, allowed actor, origin-specific completion effects.
- Worker fencing, external calls outside business locks, privacy and key/retention management.

Các invariant này phải thực thi trong service transaction/procedures được kiểm thử; không mở generic CRUD trực tiếp vào tables. Xem protocol và migration plan tại [database-design.md](database-design.md).

## 8. Validation / references

DDL và seed đã được kiểm tra trên PostgreSQL 16.2 biệt lập ở single-user mode; chi tiết test và giới hạn ở database-design.md §18. Chưa xác nhận EF migration/runtime compatibility hoặc Supabase roles/pooler. Không thay thế multi-connection concurrency/integration tests.

- [DDL](database-schema.sql)
- [Seed](seed-data.sql)
- [Movie provenance](movie-research.md)
- [PostgreSQL constraints](https://www.postgresql.org/docs/current/ddl-constraints.html)
- [Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model)
