\set ON_ERROR_STOP on
\pset pager off
\pset footer on

\echo '=== Demo PostgreSQL: movies ==='
BEGIN READ ONLY;
SET LOCAL search_path = cinema, public;

\echo ''
\echo 'Tổng số phim:'
SELECT count(*) AS movie_count
FROM movie;

\echo ''
\echo 'Danh sách phim:'
SELECT
    m.id,
    m.title,
    m.duration_minutes,
    m.status,
    COALESCE(m.language, '(chưa có)') AS language,
    COALESCE(
        string_agg(g.name, ', ' ORDER BY g.name),
        '(chưa có thể loại)'
    ) AS genres
FROM movie AS m
LEFT JOIN movie_genre AS mg ON mg.movie_id = m.id
LEFT JOIN genre AS g ON g.id = mg.genre_id
GROUP BY m.id, m.title, m.duration_minutes, m.status, m.language
ORDER BY m.id;

COMMIT;
