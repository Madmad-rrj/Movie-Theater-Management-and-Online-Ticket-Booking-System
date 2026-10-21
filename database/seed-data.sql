-- Seed script for Demo Environment (Idempotent approach)
-- Target: PostgreSQL
-- Locale/Timezone: Asia/Ho_Chi_Minh (+07)

-- 1. Reference Data: Roles
INSERT INTO role (code, name, description, is_active)
VALUES 
    ('CUSTOMER', 'Customer', 'Can view movies and book tickets', true),
    ('STAFF', 'Staff', 'Can validate tickets', true),
    ('MANAGER', 'Manager', 'Can manage schedules', true),
    ('ADMIN', 'Admin', 'System administrator', true)
ON CONFLICT (code) DO NOTHING;

-- 2. Reference Data: Genres
INSERT INTO genre (code, name, description, is_active)
VALUES 
    ('ACTION', 'Action', 'Action-packed sequences', true),
    ('SCI_FI', 'Sci-Fi', 'Science Fiction', true),
    ('ADVENTURE', 'Adventure', 'Epic journeys and exploration', true),
    ('DRAMA', 'Drama', 'Character-driven narratives', true),
    ('THRILLER', 'Thriller', 'Suspenseful plots', true),
    ('ANIMATION', 'Animation', 'Animated films', true),
    ('ROMANCE', 'Romance', 'Love stories', true),
    ('FANTASY', 'Fantasy', 'Magical and supernatural elements', true),
    ('HORROR', 'Horror', 'Scary movies', true)
ON CONFLICT (code) DO NOTHING;

-- 3. Reference Data: Seat Types & Ticket Types
INSERT INTO seat_type (code, name, capacity_units, surcharge_amount, is_active)
VALUES 
    ('STANDARD', 'Standard', 1, 0, true),
    ('VIP', 'VIP', 1, 15000, true),
    ('COUPLE', 'Couple', 2, 40000, true)
ON CONFLICT (code) DO NOTHING;

INSERT INTO ticket_type (code, name, discount_percent, eligibility_note, is_active)
VALUES 
    ('ADULT', 'Adult', 0.00, 'Standard admission', true),
    ('STUDENT', 'Student', 15.00, 'Requires valid student ID', true),
    ('CHILD', 'Child', 25.00, 'Under 12 years old', true)
ON CONFLICT (code) DO NOTHING;

-- 4. Cinema & Rooms
INSERT INTO cinema (code, name, address, phone, status)
VALUES ('CINEMA_HCM_01', 'Demo Cinema HCM', 'District 1, HCMC', '0901234567', 'active')
ON CONFLICT (code) DO NOTHING;

DO $$ 
DECLARE
    v_cinema_id bigint;
BEGIN
    SELECT id INTO v_cinema_id FROM cinema WHERE code = 'CINEMA_HCM_01';

    INSERT INTO cinema_room (cinema_id, code, name, status)
    VALUES 
        (v_cinema_id, 'ROOM_01', 'Room 1 - Standard', 'active'),
        (v_cinema_id, 'ROOM_02', 'Room 2 - VIP', 'active')
    ON CONFLICT (cinema_id, code) DO NOTHING;
END $$;

-- 5. Seats (Idempotent loop with constraint handling)
DO $$ 
DECLARE
    v_cinema_id bigint;
    v_room1_id bigint;
    v_room2_id bigint;
    v_standard_id bigint;
    v_vip_id bigint;
    v_couple_id bigint;
    r int;
    c int;
    r_char varchar(1);
BEGIN
    SELECT id INTO v_cinema_id FROM cinema WHERE code = 'CINEMA_HCM_01';
    -- Robust lookup: match both cinema_id and code
    SELECT id INTO v_room1_id FROM cinema_room WHERE cinema_id = v_cinema_id AND code = 'ROOM_01';
    SELECT id INTO v_room2_id FROM cinema_room WHERE cinema_id = v_cinema_id AND code = 'ROOM_02';
    SELECT id INTO v_standard_id FROM seat_type WHERE code = 'STANDARD';
    SELECT id INTO v_vip_id FROM seat_type WHERE code = 'VIP';
    SELECT id INTO v_couple_id FROM seat_type WHERE code = 'COUPLE';

    -- Bat buoc: dung script neu tham chieu bi thieu (thay vi de choi im lang)
    IF v_cinema_id IS NULL THEN
        RAISE EXCEPTION 'Cinema CINEMA_HCM_01 not found';
    END IF;
    IF v_room1_id IS NULL OR v_room2_id IS NULL THEN
        RAISE EXCEPTION 'Cinema rooms ROOM_01/ROOM_02 not found for cinema %', v_cinema_id;
    END IF;
    IF v_standard_id IS NULL OR v_vip_id IS NULL OR v_couple_id IS NULL THEN
        RAISE EXCEPTION 'One or more seat types (STANDARD/VIP/COUPLE) are missing';
    END IF;

    -- Room 1 (Standard & Couple)
    FOR r IN 1..5 LOOP
        r_char := chr(64 + r); -- A to E
        FOR c IN 1..10 LOOP
            IF r = 5 AND c > 5 THEN
                INSERT INTO seat (cinema_room_id, seat_type_id, row_label, seat_number, status)
                VALUES (v_room1_id, v_couple_id, r_char, c, 'active')
                ON CONFLICT ON CONSTRAINT uq_seat_position DO NOTHING;
            ELSE
                INSERT INTO seat (cinema_room_id, seat_type_id, row_label, seat_number, status)
                VALUES (v_room1_id, v_standard_id, r_char, c, 'active')
                ON CONFLICT ON CONSTRAINT uq_seat_position DO NOTHING;
            END IF;
        END LOOP;
    END LOOP;

    -- Room 2 (VIP & Couple)
    FOR r IN 1..4 LOOP
        r_char := chr(64 + r); -- A to D
        FOR c IN 1..8 LOOP
            IF r = 4 AND c > 4 THEN
                INSERT INTO seat (cinema_room_id, seat_type_id, row_label, seat_number, status)
                VALUES (v_room2_id, v_couple_id, r_char, c, 'active')
                ON CONFLICT ON CONSTRAINT uq_seat_position DO NOTHING;
            ELSE
                INSERT INTO seat (cinema_room_id, seat_type_id, row_label, seat_number, status)
                VALUES (v_room2_id, v_vip_id, r_char, c, 'active')
                ON CONFLICT ON CONSTRAINT uq_seat_position DO NOTHING;
            END IF;
        END LOOP;
    END LOOP;
END $$;

-- 6. Movies
-- LUU Y (da xac minh voi database-schema.sql): bang movie KHONG co unique constraint nao
-- tren title (chi co ck_movie_title_nonblank). Nghia la:
--   * IF NOT EXISTS chi an toan khi chay TUUAN TU (mot tien trinh). Hai len chay dong thoi
--     co the tao ban ghi cung ten vi khong co constraint de ON CONFLICT.
--   * Neu da co phim cung ten nhung du lieu sai, script se KHONG cap nhat (check-then-insert).
-- Tuy chon de gianh cho moi truong demo neu can an toan tuyet doi, them truoc doan nay:
--     CREATE UNIQUE INDEX IF NOT EXISTS ux_movie_title_normalized ON movie (lower(btrim(title)));
--   va doi sang dang: INSERT ... ON CONFLICT (lower(btrim(title))) DO UPDATE SET
--     duration_minutes = EXCLUDED.duration_minutes, ... (upsert).
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'Avengers: Endgame') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('Avengers: Endgame', 'After Thanos erased half of the universe, the remaining Avengers band together to undo his actions and restore balance.', 181, '2019-04-26', 'https://image.tmdb.org/t/p/original/or06FN3Dka5tukK1e9sl16pB3iy.jpg', 'https://www.youtube.com/watch?v=TcMBFSGVi1c', 'English', 'PG-13', 'now_showing');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'Interstellar') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('Interstellar', 'A team of astronauts travels through a wormhole in search of a planet that might support mankind.', 169, '2014-11-07', 'https://media.themoviedb.org/t/p/w600_and_h900_face/yQvGrMoipbRoddT0ZR8tPoR7NfX.jpg', 'https://www.youtube.com/watch?v=zSWdZVtXT7E', 'English', 'PG-13', 'now_showing');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'Mission: Impossible - The Final Reckoning') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('Mission: Impossible - The Final Reckoning', 'Ethan Hunt faces his ultimate mission in a race against time.', 169, '2025-05-23', 'https://media.themoviedb.org/t/p/w600_and_h900_face/iKPsC9EFUafRP9SrUznI61getVP.jpg', 'https://www.youtube.com/watch?v=NOhDyUmT9z0', 'English', 'PG-13', 'now_showing');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'Your Name') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('Your Name', 'Two teenagers discover they are swapping bodies and must work together to avert a disaster.', 106, '2016-08-26', 'https://media.themoviedb.org/t/p/w600_and_h900_face/vfJFJPepRKapMd5G2ro7klIRysq.jpg', 'https://www.youtube.com/watch?v=xU47nhruN-Q', 'Japanese', 'PG', 'now_showing');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'Kingdom of Heaven') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('Kingdom of Heaven', 'Balian of Ibelin travels to Jerusalem during the Crusades of the 12th century, and there he finds himself as the defender of the city and its people.', 144, '2005-05-06', 'https://media.themoviedb.org/t/p/w600_and_h900_face/2NaFdWy9hkM35vZB1hkqsnTcms8.jpg', 'https://www.youtube.com/watch?v=13n48NlVOnM', 'English', 'R', 'now_showing');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM movie WHERE title = 'World War Z') THEN
        INSERT INTO movie (title, description, duration_minutes, release_date, poster_url, trailer_url, language, age_rating, status)
        VALUES ('World War Z', 'Former United Nations employee Gerry Lane traverses the world in a race against time to stop the Zombie pandemic that is toppling armies and governments.', 116, '2013-06-21', 'https://media.themoviedb.org/t/p/w600_and_h900_face/aCnVdvExw6UWSeQfr0tUH3jr4qG.jpg', 'https://www.youtube.com/watch?v=HcwTxRuq-uk', 'English', 'PG-13', 'now_showing');
    END IF;
END $$;

-- 7. Movie Genres (Safe Linking)
DO $$
DECLARE
    v_movie_id bigint;
    v_genre_id bigint;
BEGIN
    SELECT id INTO v_movie_id FROM movie WHERE title = 'Avengers: Endgame' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('ACTION', 'SCI_FI', 'ADVENTURE')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;

    SELECT id INTO v_movie_id FROM movie WHERE title = 'Interstellar' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('SCI_FI', 'ADVENTURE', 'DRAMA')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;

    SELECT id INTO v_movie_id FROM movie WHERE title = 'Mission: Impossible - The Final Reckoning' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('ACTION', 'THRILLER', 'ADVENTURE')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;

    SELECT id INTO v_movie_id FROM movie WHERE title = 'Your Name' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('ANIMATION', 'ROMANCE', 'FANTASY')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;

    SELECT id INTO v_movie_id FROM movie WHERE title = 'Kingdom of Heaven' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('ACTION', 'ADVENTURE', 'DRAMA')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;

    SELECT id INTO v_movie_id FROM movie WHERE title = 'World War Z' LIMIT 1;
    IF v_movie_id IS NOT NULL THEN
        FOR v_genre_id IN (SELECT id FROM genre WHERE code IN ('HORROR', 'ACTION', 'THRILLER')) LOOP
            INSERT INTO movie_genre (movie_id, genre_id) VALUES (v_movie_id, v_genre_id) ON CONFLICT DO NOTHING;
        END LOOP;
    END IF;
END $$;

-- 8. Showtimes (Idempotent generation for demo period)
DO $$
DECLARE
    v_cinema_id bigint;
    v_room1_id bigint;
    v_room2_id bigint;
    m1 record; m2 record; m3 record; m4 record; m5 record; m6 record;
    d date;
    base_tz varchar := 'Asia/Ho_Chi_Minh';
    v_starts_at timestamptz;
BEGIN
    SELECT id INTO v_cinema_id FROM cinema WHERE code = 'CINEMA_HCM_01';
    SELECT id INTO v_room1_id FROM cinema_room WHERE cinema_id = v_cinema_id AND code = 'ROOM_01';
    SELECT id INTO v_room2_id FROM cinema_room WHERE cinema_id = v_cinema_id AND code = 'ROOM_02';

    -- Bat buoc: dung script neu phong bi thieu
    IF v_room1_id IS NULL OR v_room2_id IS NULL THEN
        RAISE EXCEPTION 'Cinema rooms not found - run the seat/room section first';
    END IF;

    SELECT id, title, duration_minutes INTO m1 FROM movie WHERE title = 'Avengers: Endgame' LIMIT 1;
    SELECT id, title, duration_minutes INTO m2 FROM movie WHERE title = 'Interstellar' LIMIT 1;
    SELECT id, title, duration_minutes INTO m3 FROM movie WHERE title = 'Mission: Impossible - The Final Reckoning' LIMIT 1;
    SELECT id, title, duration_minutes INTO m4 FROM movie WHERE title = 'Your Name' LIMIT 1;
    SELECT id, title, duration_minutes INTO m5 FROM movie WHERE title = 'Kingdom of Heaven' LIMIT 1;
    SELECT id, title, duration_minutes INTO m6 FROM movie WHERE title = 'World War Z' LIMIT 1;

    -- Ensure movies exist before attempting showtime generation
    IF m1.id IS NULL OR m2.id IS NULL OR m3.id IS NULL OR m4.id IS NULL OR m5.id IS NULL OR m6.id IS NULL THEN
        RAISE NOTICE 'Skipping showtime generation: One or more movies are missing.';
        RETURN;
    END IF;

    -- Loop through 8 days (Oct 28 to Nov 4, 2026)
    FOR i IN 0..7 LOOP
        d := '2026-10-28'::date + i;

        -- ====== ROOM 1 ======
        
        -- 10:00 - Your Name (106 min)
        v_starts_at := (d || ' 10:00:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m4.id, v_room1_id, m4.title, 'Room 1 - Standard', m4.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m4.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m4.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 80000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;

        -- 13:00 - Avengers (181 min)
        v_starts_at := (d || ' 13:00:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m1.id, v_room1_id, m1.title, 'Room 1 - Standard', m1.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m1.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m1.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 85000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;

        -- 17:00 - Mission Impossible (169 min)
        v_starts_at := (d || ' 17:00:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m3.id, v_room1_id, m3.title, 'Room 1 - Standard', m3.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m3.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m3.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 90000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;

        -- ====== ROOM 2 ======

        -- 11:00 - World War Z (116 min)
        v_starts_at := (d || ' 11:00:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m6.id, v_room2_id, m6.title, 'Room 2 - VIP', m6.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m6.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m6.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 100000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;

        -- 14:30 - Kingdom of Heaven (144 min)
        v_starts_at := (d || ' 14:30:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m5.id, v_room2_id, m5.title, 'Room 2 - VIP', m5.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m5.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m5.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 100000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;

        -- 18:00 - Interstellar (169 min)
        v_starts_at := (d || ' 18:00:00 ' || base_tz)::timestamptz;
        INSERT INTO showtime (movie_id, cinema_room_id, movie_title_snapshot, room_name_snapshot, duration_minutes_snapshot, starts_at, ends_at, room_available_at, booking_closes_at, base_price, status)
            VALUES (m2.id, v_room2_id, m2.title, 'Room 2 - VIP', m2.duration_minutes,
                    v_starts_at,
                    v_starts_at + (m2.duration_minutes || ' minutes')::interval,
                    v_starts_at + ((m2.duration_minutes + 15) || ' minutes')::interval,
                    v_starts_at - interval '10 minutes', 110000, 'scheduled')
        ON CONFLICT ON CONSTRAINT ex_showtime_room_time DO NOTHING;
    END LOOP;
END $$;