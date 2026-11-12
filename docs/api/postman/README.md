# BE2 Postman import

Import these two files through Postman **Import → Files**:

- `Cinema-BE2.postman_collection.json`: 14 requests for the implemented Guest, selection and reservation APIs.
- `Cinema-BE2-Local.postman_environment.json`: local configuration template.

Select **Cinema BE2 Local** in the environment selector. Keep the default
`baseUrl=http://127.0.0.1:5080` and `origin=http://127.0.0.1:5173` for the existing
Arch Docker profile. Restart the API after source changes using the instructions
in [selection-quote.md](../../development/selection-quote.md).

Set these environment values before running:

| Variable | Value |
|---|---|
| `showtimeId` | Real decimal-string ID of a future, scheduled showtime in your local DB. |
| `seatIds` | JSON array of decimal strings for seats in that showtime's room, for example `["456"]`. Example IDs are placeholders, not seeded IDs to copy. |
| `adult` / `discounted` | Nonnegative integer quantities with total 1–8 by default; total must cover the selected seat count. |
| `updateSeatIds` | Defaults to `[]` to test deselection; optionally set to another valid JSON seat array. |

The other variables are populated by response scripts. Templates contain no live
cookie, CSRF token, credentials, or database connection strings. Postman retains
`cinema_guest` in its cookie jar and stores the returned CSRF token in your selected
local environment. Keep cookies enabled and do not add an Authorization header.
Do not export/share your populated environment or cookies; import the blank
committed template for a fresh workspace.

BE1 showtime/seat discovery endpoints are not implemented here. To get actual IDs
from your **local** Docker database, run from the repository root (with the same
`CINEMA_BE2_DATA_DIR` environment used when starting the stack):

```sh
docker compose -p cinema-be2 -f docker-compose.be2.yml exec -T postgres sh -c \
  'exec psql -X -U "$POSTGRES_USER" -d "$POSTGRES_DB"' <<'SQL'
SELECT s.id AS showtime_id, se.id AS seat_id, s.starts_at,
       se.row_label, se.seat_number
FROM cinema.showtime s
JOIN cinema.seat se ON se.cinema_room_id = s.cinema_room_id
JOIN cinema.seat_type st ON st.id = se.seat_type_id
WHERE s.status = 'scheduled' AND s.starts_at > clock_timestamp()
  AND se.operational_state = 'active' AND se.retired_at IS NULL
  AND st.is_active
  AND NOT EXISTS (SELECT 1 FROM cinema.booking_seat bs
                  WHERE bs.showtime_id=s.id AND bs.seat_id=se.id
                    AND bs.allocation_status IN ('held','sold'))
ORDER BY s.starts_at, s.id, se.id
LIMIT 20;
SQL
```

Choose one `showtime_id` and seat IDs from that showtime's rows. The existing demo
seed is dated November 14–21, 2026. An empty result requires a reviewed local
fixture with future showtimes; do not bypass production time checks or reset a
shared database to make a test work.

Run requests **01 through 14 in order within five minutes**, individually or with
Collection Runner:

1. Bootstrap Guest and capture the CSRF token; a cookie is retained automatically.
2. Replay the same Guest operation.
3. Read the existing movie-list API.
4. Create a selection (or reuse an active one for this Guest/showtime).
5. Replay that create request using its same key and payload.
6. Read selection/quotation and verify the deadline stayed unchanged.
7. Update using the captured version; preserve the old version for the next check.
8. Expect **409 VERSION_CONFLICT** for the stale version.
9. Expect **403 CSRF_REJECTED** when the CSRF header is omitted.
10. Restore the complete physical-seat set from `seatIds` and capture the latest quote/version.
11. Reserve one pending booking using that quote and synthetic contact; keep the original deadline.
12. Replay the reservation key/payload and expect the same booking, with no duplicate holds.
13. Expect **409 IDEMPOTENCY_KEY_CONFLICT** for an altered accepted amount with that same key.
14. Expect **409 STATE_CONFLICT** when trying to reserve the consumed selection with a new key.

Requests 08, 09, 13 and 14 are deliberate negative tests; those error responses mean the
checks passed. Create requests generate new UUID keys, while replay requests reuse
the preceding key. If the selection expires mid-run, rerun from request 01; a new
create closes the expired active selection and starts a new valid session.

For deeper tests of ownership, price changes, unavailable seats and expiration,
run `python3 scripts/test-guest-context.py`. Database time/price mutation fixtures
are confined to that disposable test suite, not Postman requests against shared
or production data. Requests 11–14 exercise real local booking/hold behavior. This
collection creates no payments, MoMo calls, tickets or emails. Until task 5 adds safe
expiry/cancellation, existing held allocations remain conflicts even after their
deadline. Choose other available seats when repeating; do not reset shared data.

The environment also supplies `contactFullName`, `contactPhone` and `contactEmail`
with synthetic demo values. `quoteId`, `quoteVersion`, `acceptedTotal`,
`bookingRequestKey` and `bookingId` are populated by requests 10–11. The selected
seat count must equal adult + discounted before reservation; request 11 refuses an
incomplete setup. Detailed behavior: [booking-reservation.md](../../development/booking-reservation.md).
