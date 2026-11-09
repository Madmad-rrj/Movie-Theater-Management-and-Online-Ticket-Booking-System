-- Promethe-Phos demo fixture v2; matches cinema schema in database-schema.sql.
-- Run only in an isolated development database after reviewing database-design.md.
-- Pure PostgreSQL SQL; psql -X -v ON_ERROR_STOP=1 -f seed-data.sql
-- Optional, BEFORE running: SET cinema.demo_start_date = '2026-11-14';
-- Default is pinned (not CURRENT_DATE), so retries do not add a moving date window.
-- Re-running the same fixture/date preserves existing data; drift causes rollback.
-- Does not seed passwords, users, paid bookings, fake payments, admissions or emails.
BEGIN;
SET LOCAL search_path = cinema, public;
SET LOCAL TIME ZONE 'UTC';
SELECT pg_advisory_xact_lock(726141901);

CREATE TEMP TABLE demo_movies (
    seed_key text PRIMARY KEY, title text, runtime integer, description text, language text
) ON COMMIT DROP;
INSERT INTO demo_movies VALUES
('DEMO_ENDGAME','Avengers: Endgame',181,'Những người hùng còn lại tìm cách đảo ngược hậu quả từ cuộc đối đầu với Thanos.','English'),
('DEMO_INTERSTELLAR','Interstellar',169,'Một đoàn thám hiểm tìm kiếm tương lai cho con người ngoài hệ Mặt Trời.','English'),
('DEMO_MI_FINAL','Mission: Impossible - The Final Reckoning',169,'Ethan Hunt cùng đồng đội đối diện một nhiệm vụ đe dọa tương lai thế giới.','English'),
('DEMO_YOUR_NAME','Your Name',106,'Hai người trẻ khám phá sự kết nối kỳ lạ vượt qua khoảng cách và thời gian.','Japanese'),
('DEMO_KINGDOM','Kingdom of Heaven',144,'Một người thợ rèn bị cuốn vào những xung đột quanh Jerusalem thời Trung cổ.','English'),
('DEMO_WWZ','World War Z',116,'Một cựu nhân viên Liên Hợp Quốc tìm lời giải cho một đại dịch toàn cầu.','English');

-- Required Identity roles; code/name/normalized_name must be mapped in EF Identity.
INSERT INTO role(code,name,normalized_name,concurrency_stamp)
SELECT x,x,x,'demo-role-v2-' || x FROM unnest(ARRAY['CUSTOMER','STAFF','MANAGER','ADMIN']) AS x
ON CONFLICT (code) DO NOTHING;

CREATE TEMP TABLE demo_genres(code text PRIMARY KEY,name text) ON COMMIT DROP;
INSERT INTO demo_genres VALUES
('ACTION','Action'),('SCI_FI','Science Fiction'),('ADVENTURE','Adventure'),
('DRAMA','Drama'),('THRILLER','Thriller'),('ANIMATION','Animation'),
('ROMANCE','Romance'),('FANTASY','Fantasy'),('HORROR','Horror');
INSERT INTO genre(code,name,normalized_name,is_active)
SELECT code,name,upper(name),true FROM demo_genres
ON CONFLICT (code) DO NOTHING;

INSERT INTO seat_type(code,name,surcharge_amount,is_active) VALUES
('DEMO_STANDARD','Standard',0,true),('DEMO_VIP','VIP',15000,true)
ON CONFLICT (code) DO NOTHING;

INSERT INTO cinema(code,name,address,timezone,status,minimum_turnaround_minutes)
VALUES ('DEMO_CINEMA_HCM_01','Promethe-Phos Demo Cinema','Synthetic demo location, Ho Chi Minh City','Asia/Ho_Chi_Minh','active',15)
ON CONFLICT (code) DO NOTHING;

INSERT INTO cinema_room(cinema_id,code,name,status)
SELECT c.id,r.code,r.name,'active'
FROM cinema c CROSS JOIN (VALUES ('DEMO_ROOM_01','Demo Room 1'),('DEMO_ROOM_02','Demo Room 2')) r(code,name)
WHERE c.code = 'DEMO_CINEMA_HCM_01'
ON CONFLICT (cinema_id,code) DO NOTHING;

INSERT INTO cinema_ticket_price(cinema_id,category_code,amount,currency)
SELECT c.id,p.category,p.amount,'VND'
FROM cinema c CROSS JOIN (VALUES ('Adult',100000),('Discounted',70000)) p(category,amount)
WHERE c.code = 'DEMO_CINEMA_HCM_01'
ON CONFLICT (cinema_id,category_code) DO NOTHING;

-- 5 rows x 10 seats in each of two rooms: 100 physical seats, one entitlement each.
INSERT INTO seat(cinema_room_id,seat_type_id,row_label,seat_number,x_position,y_position,operational_state)
SELECT r.id,st.id,chr(64+row_no),seat_no,seat_no-1,row_no-1,'active'
FROM cinema c JOIN cinema_room r ON r.cinema_id=c.id
CROSS JOIN generate_series(1,5) row_no CROSS JOIN generate_series(1,10) seat_no
JOIN seat_type st ON st.code=CASE WHEN row_no>=4 THEN 'DEMO_VIP' ELSE 'DEMO_STANDARD' END
WHERE c.code='DEMO_CINEMA_HCM_01' AND r.code IN ('DEMO_ROOM_01','DEMO_ROOM_02')
ON CONFLICT (cinema_room_id,row_label,seat_number) DO NOTHING;

-- now_showing is fictional DEMO catalogue state, not a real 2026 release claim.
-- Release date/market, age classification and external media are deliberately NULL
-- until the exact edition/territory/assets are approved; see movie-research.md.
INSERT INTO movie(seed_key,title,description,duration_minutes,language,status)
SELECT seed_key,title,description,runtime,language,'now_showing' FROM demo_movies
ON CONFLICT (seed_key) DO NOTHING;

CREATE TEMP TABLE demo_movie_genres(seed_key text,genre_code text,PRIMARY KEY(seed_key,genre_code)) ON COMMIT DROP;
INSERT INTO demo_movie_genres VALUES
('DEMO_ENDGAME','ACTION'),('DEMO_ENDGAME','SCI_FI'),('DEMO_ENDGAME','ADVENTURE'),
('DEMO_INTERSTELLAR','SCI_FI'),('DEMO_INTERSTELLAR','ADVENTURE'),('DEMO_INTERSTELLAR','DRAMA'),
('DEMO_MI_FINAL','ACTION'),('DEMO_MI_FINAL','THRILLER'),('DEMO_MI_FINAL','ADVENTURE'),
('DEMO_YOUR_NAME','ANIMATION'),('DEMO_YOUR_NAME','ROMANCE'),('DEMO_YOUR_NAME','FANTASY'),
('DEMO_KINGDOM','ACTION'),('DEMO_KINGDOM','ADVENTURE'),('DEMO_KINGDOM','DRAMA'),
('DEMO_WWZ','HORROR'),('DEMO_WWZ','ACTION'),('DEMO_WWZ','THRILLER');
INSERT INTO movie_genre(movie_id,genre_id)
SELECT m.id,g.id FROM demo_movie_genres d JOIN movie m ON m.seed_key=d.seed_key JOIN genre g ON g.code=d.genre_code
ON CONFLICT (movie_id,genre_id) DO NOTHING;

-- Drift detection BEFORE scheduling: never silently overwrite edited business data.
DO $$
BEGIN
  IF (SELECT count(*) FROM ticket_category) <> 2 THEN RAISE EXCEPTION 'Exactly two ticket categories required'; END IF;
  IF EXISTS (SELECT 1 FROM role WHERE name <> code OR normalized_name <> code) THEN
    RAISE EXCEPTION 'Role mapping drift; review Identity mapping instead of overwriting'; END IF;
  IF EXISTS (SELECT 1 FROM demo_movies d LEFT JOIN movie m ON m.seed_key=d.seed_key
             WHERE m.id IS NULL OR m.title<>d.title OR m.duration_minutes<>d.runtime OR m.status<>'now_showing') THEN
    RAISE EXCEPTION 'Demo movie fixture drift; no metadata overwrite performed'; END IF;
  IF EXISTS (SELECT 1 FROM demo_genres d JOIN genre g ON g.code=d.code WHERE NOT g.is_active OR g.normalized_name<>upper(d.name)) THEN
    RAISE EXCEPTION 'Genre fixture drift'; END IF;
  IF EXISTS (SELECT 1 FROM seat_type WHERE code IN ('DEMO_STANDARD','DEMO_VIP') AND
    (NOT is_active OR surcharge_amount <> CASE code WHEN 'DEMO_VIP' THEN 15000 ELSE 0 END)) THEN
    RAISE EXCEPTION 'Seat type fixture drift'; END IF;
  IF EXISTS (SELECT 1 FROM cinema WHERE code='DEMO_CINEMA_HCM_01' AND
    (status<>'active' OR timezone<>'Asia/Ho_Chi_Minh' OR minimum_turnaround_minutes<>15)) THEN
    RAISE EXCEPTION 'Cinema fixture drift'; END IF;
  IF EXISTS (SELECT 1 FROM cinema_room r JOIN cinema c ON c.id=r.cinema_id
    WHERE c.code='DEMO_CINEMA_HCM_01' AND r.code IN ('DEMO_ROOM_01','DEMO_ROOM_02') AND r.status<>'active') THEN
    RAISE EXCEPTION 'Room fixture drift'; END IF;
  IF EXISTS (SELECT 1 FROM cinema_ticket_price p JOIN cinema c ON c.id=p.cinema_id
    WHERE c.code='DEMO_CINEMA_HCM_01' AND p.amount <> CASE p.category_code WHEN 'Adult' THEN 100000 ELSE 70000 END) THEN
    RAISE EXCEPTION 'Cinema price fixture drift'; END IF;
  IF EXISTS (SELECT 1 FROM seat s JOIN cinema_room r ON r.id=s.cinema_room_id JOIN cinema c ON c.id=r.cinema_id JOIN seat_type st ON st.id=s.seat_type_id
    WHERE c.code='DEMO_CINEMA_HCM_01' AND r.code IN ('DEMO_ROOM_01','DEMO_ROOM_02') AND
    (s.retired_at IS NOT NULL OR s.operational_state<>'active' OR s.row_label NOT IN ('A','B','C','D','E') OR s.seat_number>10 OR
     s.x_position<>s.seat_number-1 OR s.y_position<>ascii(s.row_label)-65 OR
     st.code<>CASE WHEN s.row_label IN ('D','E') THEN 'DEMO_VIP' ELSE 'DEMO_STANDARD' END)) THEN
    RAISE EXCEPTION 'Physical seat fixture drift'; END IF;
END $$;

CREATE TEMP TABLE demo_schedule (
  seed_key text PRIMARY KEY, movie_id bigint, room_id bigint, duration integer,
  starts_at timestamptz, ends_at timestamptz, room_available_at timestamptz
) ON COMMIT DROP;
INSERT INTO demo_schedule
SELECT 'DEMO_V2_' || d.day::text || '_' || slot.room_code || '_' || slot.hhmm,
       m.id,r.id,m.duration_minutes,x.start_time,
       x.start_time+m.duration_minutes*interval '1 minute',
       x.start_time+(m.duration_minutes+15)*interval '1 minute'
FROM (SELECT coalesce(nullif(current_setting('cinema.demo_start_date',true),''),'2026-11-14')::date + n AS day FROM generate_series(0,7) n) d
CROSS JOIN (VALUES
 ('DEMO_ROOM_01','10:00','DEMO_YOUR_NAME'),('DEMO_ROOM_01','13:00','DEMO_ENDGAME'),('DEMO_ROOM_01','17:00','DEMO_MI_FINAL'),
 ('DEMO_ROOM_02','11:00','DEMO_WWZ'),('DEMO_ROOM_02','14:30','DEMO_KINGDOM'),('DEMO_ROOM_02','18:00','DEMO_INTERSTELLAR')
) slot(room_code,hhmm,movie_key)
JOIN cinema c ON c.code='DEMO_CINEMA_HCM_01'
JOIN cinema_room r ON r.cinema_id=c.id AND r.code=slot.room_code
JOIN movie m ON m.seed_key=slot.movie_key
CROSS JOIN LATERAL (SELECT (d.day+slot.hhmm::time) AT TIME ZONE 'Asia/Ho_Chi_Minh' AS start_time) x;

-- Only the stable seed key is a replay conflict. Overlap is an ERROR, not silently skipped.
INSERT INTO showtime(seed_key,movie_id,cinema_room_id,duration_minutes_snapshot,turnaround_minutes_applied,starts_at,ends_at,room_available_at,status)
SELECT seed_key,movie_id,room_id,duration,15,starts_at,ends_at,room_available_at,'scheduled' FROM demo_schedule
ON CONFLICT (seed_key) DO NOTHING;

DO $$
BEGIN
 IF (SELECT count(*) FROM demo_schedule) <> 48 THEN RAISE EXCEPTION 'Expected 48 fixture showtimes'; END IF;
 IF EXISTS (SELECT 1 FROM demo_schedule d LEFT JOIN showtime s ON s.seed_key=d.seed_key
   WHERE s.id IS NULL OR s.movie_id<>d.movie_id OR s.cinema_room_id<>d.room_id OR s.starts_at<>d.starts_at OR
         s.ends_at<>d.ends_at OR s.room_available_at<>d.room_available_at OR s.duration_minutes_snapshot<>d.duration OR
         s.turnaround_minutes_applied<>15 OR s.status<>'scheduled') THEN
   RAISE EXCEPTION 'Existing demo showtime drift; do not reset or reactivate existing schedules'; END IF;
END $$;
COMMIT;
-- Outcome on a fresh baseline: 4 roles, 2 categories, 9 genres, 6 movies,
-- 18 associations, 1 demo cinema, 2 rooms, 100 seats, 2 cinema prices, 48 showtimes.
-- Changing demo_start_date explicitly creates another date set; old rows stay intact.
