# BE2 Postman import

Import these two files through Postman **Import → Files**:

- `Cinema-BE2.postman_collection.json`: 30 requests for Guest, selection, reservation, recovery, cancellation and expiry.
- `Cinema-BE2-Local.postman_environment.json`: local configuration template.

Select **Cinema BE2 Local** in the environment selector. Keep the default
`baseUrl=http://127.0.0.1:5080` and `origin=http://127.0.0.1:5173` for the existing
Arch Docker profile. Restart the API after source changes using the instructions
in [selection-quote.md](../../development/selection-quote.md).

If you imported an earlier version, import the collection again and replace the
old copy when prompted. The current collection is named **Cinema BE2 — Task 5 —
Checkout, Cancellation and Expiry**, revision **1.5.0**, with two folders. Postman
does not automatically reload repository changes. Keep your existing local
environment to preserve your configured IDs, or import the environment template
for a fresh setup. Run from request 01 after replacing the collection.

Set these environment values before running:

| Variable | Value |
|---|---|
| `showtimeId` | Real decimal-string ID of a future, scheduled showtime in your local DB. |
| `seatIds` | JSON array of decimal strings for seats in that showtime's room, for example `["456"]`. Example IDs are placeholders, not seeded IDs to copy. |
| `adult` / `discounted` | Nonnegative integer quantities with total 1–8 by default; total must cover the selected seat count. |
| `updateSeatIds` | Defaults to `[]` to test deselection; optionally set to another valid JSON seat array. |
| `contactEmail` | Valid plain email text, e.g. `guest@example.invalid`; no surrounding quotes/spaces or unresolved variable reference. |

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

Run folder **01–26 Main flow** with Collection Runner for the fast path. Individual
Send also works, but complete reservation/cancellation before the original
five-minute deadline so the worker does not expire the booking first. Recovery
remains readable afterwards and reflects the current state. Running the whole
collection also runs the optional expiry folder, which waits about five minutes.

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
11. Reserve one pending booking using that quote and synthetic contact; verify seats, quantities, accepted total and the original deadline, with no payment started.
12. Replay the reservation key/payload and expect the same booking, seats, total and deadline, with no duplicate holds.
13. Expect **409 IDEMPOTENCY_KEY_CONFLICT** for an altered accepted amount with that same key.
14. Expect **409 STATE_CONFLICT** when trying to reserve the consumed selection with a new key.
15. Read the existing booking; verify its ID, contact, committed total, version and deadline.
16. Recover checkout with that same Guest cookie; expect `pay` while eligible or `selectAgain` after the unpaid deadline, and no payment row.
17. Reload checkout; verify the same booking, snapshots, version and deadline again.
18. Expect **401 UNAUTHENTICATED** with cookies disabled for this request only. The saved Guest cookie is retained.
19. Expect **404 NOT_FOUND** for an intentionally missing booking ID; no private data is returned.
20. Expect **400 VALIDATION_ERROR** without explicit cancellation confirmation.
21. Expect **403 CSRF_REJECTED** for cancellation without CSRF.
22. Cancel the pending booking; verify the same ID, immutable snapshots/deadline and one version increment.
23. Replay that cancellation key; expect 200, a replay marker and unchanged cancelled version.
24. Recover the cancelled booking; expect `selectAgain`.
25. Expect **422 CANCELLATION_NOT_ELIGIBLE** for a new cancellation intent after cancellation.
26. Replay that rejected intent; expect the same 422 with a replay marker.

Requests 08, 09, 13, 14, 18–21, 25 and 26 are deliberate negative tests; those error responses mean the
checks passed. Create requests generate new UUID keys, while replay requests reuse
the preceding key. If the selection expires mid-run, rerun from request 01; a new
create closes the expired active selection and starts a new valid session.

For deeper tests of ownership, price changes, unavailable seats and expiration,
run `python3 scripts/test-guest-context.py`. Database time/price mutation fixtures
are confined to that disposable test suite, not Postman requests against shared
or production data. Requests 11–14 exercise real local booking/hold behavior. This
collection creates no payments, MoMo calls, tickets or emails. The main folder
ends with a cancelled booking and released seats. With the local task-5 worker
enabled, eligible overdue unpaid pending holds are also released automatically;
unresolved payments remain protected. Do not reset shared data for a test.

Folder **27–30 Optional natural expiry** runs after the main folder with the same
environment/cookie. It creates another selection on the released seats, reserves
again, waits/polls for backend-persisted `expired`, then reads the expired history.
In Collection Runner/Newman request 29 repeats once per second, bounded by
`expiryWaitSeconds` (default **420**). With Send, revisit request 29 after the
original deadline plus a worker sweep. This does not shorten timers or invoke a
worker endpoint. Keep the API running and enable its expiry worker; default local
polling is 15 seconds. For longer configured intervals/backlogs, increase the wait
limit. Optional flow replaces the current booking variables while retaining the
first ID in `cancelledBookingId`.

The environment also supplies `contactFullName`, `contactPhone` and `contactEmail`
with synthetic demo values. `quoteId`, `quoteVersion`, `acceptedTotal`,
`bookingRequestKey` and `bookingId` are populated by requests 10–11. The selected
seat count must equal adult + discounted before reservation; request 11 refuses an
incomplete setup. `bookingVersion` is also captured by request 11. Request 01 clears
old captured selection/quote/booking values so a failed create cannot reuse stale
state. A 404 on request 10 requires a successful request 04 with the same Guest
cookie/environment; an invalid `Contact.Email` on request 11 requires correcting
`contactEmail` in that environment. Detailed behavior:
[booking-reservation.md](../../development/booking-reservation.md) and
[checkout-recovery.md](../../development/checkout-recovery.md).
Cancellation keys/versions are captured automatically. Worker setup, payment
guards and cancellation outcomes: [booking-cancel-expiry.md](../../development/booking-cancel-expiry.md).
