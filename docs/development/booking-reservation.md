# Booking reservation — BE2 task 3

Branch `feat/be-booking-reservation` was fast-forwarded from task 2 at `b537aa5`.
The new endpoint is **POST `/api/bookings`**. A successful request creates one
`pending` booking, both category snapshots, all physical-seat `held` allocations,
selection consumption, an audit event, and the idempotency receipt in one database
transaction. It never creates payments, tickets or email notifications.

GET booking/checkout endpoints are now implemented by [task 4](checkout-recovery.md). Pending
cancellation and background expiry are implemented by [task 5](booking-cancel-expiry.md).
Elapsed timestamps alone do not release persisted holds; a conflicting held/sold seat
continues to return `SEAT_CONFLICT`. Do not reset shared data to bypass this.

## Postman / local use

Start or restart the existing Arch Docker backend as described in
[guest-checkout.md](guest-checkout.md). The existing API container copies source
at startup, so source changes require restarting it. No new external API keys,
Supabase setup, schema changes, migrations, or startup seed operations are needed.

Re-import the updated [collection](../api/postman/Cinema-BE2.postman_collection.json)
and [local environment](../api/postman/Cinema-BE2-Local.postman_environment.json).
Select **Cinema BE2 Local** and set real local future-showtime and available-seat
IDs. See the [import guide](../api/postman/README.md). Run **01–14 in order** within
the original five-minute selection period. Requests 10–14 add complete-selection
restoration, reservation, same-key replay, changed-payload conflict, and rejection
of a consumed selection with a new key. Synthetic contact values are supplied in
the environment; they are not real customer data. `quoteId`, `quoteVersion`,
`acceptedTotal`, `bookingRequestKey` and `bookingId` are captured automatically.

For an individual POST booking request, retain the Guest cookie and send:

```text
Origin: http://127.0.0.1:5173
X-CSRF-Token: <Guest bootstrap response header>
Idempotency-Key: <one UUID reused for retries of this same intent>
Content-Type: application/json
```

Request shape (use actual values returned by selection/quote, not these illustrative IDs):

```json
{
  "selectionId": "00000000-0000-4000-8000-000000000001",
  "selectionVersion": "1",
  "showtimeId": "17",
  "seatIds": ["4"],
  "quantities": { "adult": 1, "discounted": 0 },
  "contact": { "fullName": "Demo Guest", "phone": "0900000000", "email": "guest@example.invalid" },
  "declarations": { "termsAccepted": true, "ageRequirementsAcknowledged": true },
  "priceAcceptance": {
    "quoteId": "00000000-0000-4000-8000-000000000002",
    "version": "<quote version>",
    "acceptedTotal": { "amount": 100000, "currency": "VND" }
  }
}
```

Showtime, physical-seat, booking and payment database IDs are decimal strings;
selection/context/quote IDs and the public booking code are UUIDs. Public IDs and
booking codes never replace Guest ownership proof. Response is `{ "data": Booking }`.
It includes the booking ID/code/status, current showtime, purchased-start snapshot,
committed seats/prices, contact, quantities, original deadline and version. A new
booking returns 201. Recognized successful replay returns 200 and
`Idempotency-Replayed: true`, with the same booking's current state. Financial
snapshots remain unchanged if configuration changes after booking. The financial
snapshot's `quotedAt` is the booking creation time (snapshot commit), not a restored
browser timestamp. A pending total of zero is not automatically confirmed or paid;
payment eligibility/zero-total workflow remains outside task 3.

## Validation and price acceptance

- The selection must belong to the current Guest, remain active/unexpired, and
  have the supplied current selection version. Showtime, distinct seat set and
  category quantities must match that selection exactly.
- Reservation requires exactly as many physical seats as tickets, within the
  snapshotted configured maximum. Category quantities remain at booking level;
  they are never assigned to individual seats.
- Showtime/room/cinema/movie and physical-seat/type eligibility are re-read from
  PostgreSQL under locks. The deadline and Guest validity are checked again just
  before commit, including after allocation writes.
- Contact is required: trimmed nonempty name (max 150), email format (max 256),
  phone (max 30 characters, 7–15 digits, optional leading `+`, spaces, parentheses
  and hyphens). These are implemented format rules, not external email/phone
  verification. Both declarations must explicitly be true.
- Accepted quote ID/version, amount **and currency** are required. VND amounts
  must be whole and nonnegative; client amounts are never used as the price source.
- Server quotation is recalculated using database category prices and seat
  surcharges. Its fingerprint now includes showtime/movie/room/cinema identity,
  showtime version, purchased start, movie/room names, quantities and price/seat
  information. Changes require renewed acceptance rather than silently reserving.
- `PRICE_CHANGED` returns `details.quote` and `details.selectionVersion`. The
  refreshed quote/version is persisted without booking or holds. Accept that new
  quote using the latest selection version and a **new** reservation key. Neither
  rejection nor acceptance extends the original selection deadline.

## Transactions, concurrency and receipts

Selection and reservation share a per-Guest advisory lock. Lock acquisition is
Guest validity → Cinema → Room → Showtime → Selection, following that advisory
lock; booking/payment reads follow showtime. Reservation takes an update lock on
the showtime; selection takes share locks. No path locks a booking then climbs
back to showtime. Ancestor locations are rechecked after lock acquisition.
Shared BE1 entity mappings/endpoints remain untouched: reads use the existing
BE2 parameterized SQL adapter, now with explicit common lock order. BE1 and future
payment/expiry services must preserve this protocol when integrated.

All active allocations for the requested seats are checked. The existing unique
partial index `ux_active_seat` remains the final database guard. A savepoint before
booking writes allows a late unique-allocation failure or elapsed deadline to undo
booking/category/seat/consumption/audit changes. No subset of requested seats can
remain held. This task conservatively rejects existing holds even if their old
booking deadline elapsed; only task-5 payment-safe expiry may release them.

Idempotency is scoped to Guest + reserve operation + bookings target + UUID key.
The canonical fingerprint uses parsed IDs, sorted seat sets, quantities, trimmed
contact, declarations and accepted quote/money. Receipts retain no raw credential
or unnecessary contact data: success references the booking; 409/422 rejection
stores its business error/quote details. Same key with changed valid payload returns
409 `IDEMPOTENCY_KEY_CONFLICT`. Invalid-format requests are rejected before effects.

Recognized rejected replay returns its recorded error, also marked
`Idempotency-Replayed: true`; it does not retry a failed seat purchase after seats
become free or replace a stale quote with a new intent. Replay rechecks current
Guest authority and ownership. Confirmed/expired/cancelled booking replay returns
current persisted state without reactivation, new holds, repricing or timer renewal.
Retries of transient/deadlock/serialization failures use a fresh EF context and the
same operation identity. Audit, receipt and booking effects remain atomic.

## Outcomes

| HTTP | Code | Meaning |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Invalid contact, declarations, ID/key format, malformed money, missing required fields or unknown write fields. |
| 401/403 | Guest/CSRF errors | Existing Guest security policy; unsupported bearer credentials never fall back to Guest. |
| 404 | `NOT_FOUND` | Missing or foreign-owned selection/booking. |
| 409 | `VERSION_CONFLICT` | Selection version changed. |
| 409 | `STATE_CONFLICT` | Selection consumed/abandoned, or request does not match selection. |
| 409 | `SEAT_CONFLICT` | At least one requested allocation conflicts; `details.seatIds` identifies conflicts (the attempted set for a late unique failure). |
| 409 | `IDEMPOTENCY_KEY_CONFLICT` | Same key with different valid intent. |
| 422 | `SELECTION_EXPIRED` | Original selection deadline elapsed, including during writes. |
| 422 | `PRICE_CHANGED` | Renewed quote/showtime acceptance required, with current quote/version. |
| 422 | `BUSINESS_RULE_VIOLATION` | Incomplete seat selection, missing pricing or total outside SQL money range. |
| 422 | `SHOWTIME_NOT_BOOKABLE` | Current showtime/resource conditions disallow reservation. |

## Verification

```sh
python3 scripts/test-guest-context.py
```

The runner discovers Guest, Selection and BookingReservation tests together against
disposable PostgreSQL 16.4. Reservation tests exercise two-Guest seat contention,
same-key concurrent requests, edit/reserve races, immutable snapshots, altered-price
reacceptance, foreign ownership, stale/expired selections, required contact/money,
late unique-constraint failure, and Guest/selection expiry during database writes.
Injected delay/conflict triggers exist only inside that isolated test database and
are dropped afterwards; production schema/scripts are unchanged.

Read APIs/recovery are covered by [task 4](checkout-recovery.md), and safe cancellation/expiry by [task 5](booking-cancel-expiry.md). MoMo, ticketing,
email, online deployment and network/uncertain-commit fault injection are not
implemented or claimed tested here. Postman tests create real **local demo** holds;
use available seats or complete the explicit cancellation/safe expiry flow when repeating.

Verification on 2026-11-12: 60/60 integration cases passed, 0 skipped; final-source Kestrel/Newman run passed 14 requests and 30 assertions. Its disposable database contained exactly one pending booking/held seat and no payment, ticket or notification rows.

The current collection extends the original 01–14 flow with task-4 requests 15–19;
re-import it to test booking reads and recovery as well.
