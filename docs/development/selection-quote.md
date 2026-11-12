# Selection and quotation — BE2 task 2

Branch `feat/be-selection-quote` includes Guest task 1 (`104d481`). The shipped
endpoints are POST `/api/selections`, GET `/api/selections/{selectionId}`, and PUT
`/api/selections/{selectionId}`. They use `[GuestCheckout]`: cookie ownership,
current database Guest validity, trusted Origin/CSRF for writes, and no-store.
No booking, payment or seat hold is created.

## IDs and integration boundary

**FE2 contract correction:** `showtimeId` and `seatIds` use positive decimal strings,
matching the actual bigint SQL keys and BE1 instructions. Selection/context/quote
IDs remain UUIDs. JSON unknown write fields are rejected. The SQL schema and BE1
entities/endpoints are unchanged.

BE1 mappings are still absent from this checkout. `SelectionService` uses narrow,
parameterized read-only SQL projections for showtime/room/cinema/movie, physical
seats/seat types and cinema prices inside its transaction. These are BE2 reads,
not duplicate EF entity mappings or BE1 endpoint implementations. Coordinate this
adapter with BE1 when their shared read contracts are integrated; reservation
must preserve the agreed common lock hierarchy before task 3 is completed.

## Postman

Import the [collection](../api/postman/Cinema-BE2.postman_collection.json) and
[local environment template](../api/postman/Cinema-BE2-Local.postman_environment.json),
then follow the [import/run guide](../api/postman/README.md).

Run the existing `docker-compose.be2.yml` backend as described in
[guest-checkout.md](guest-checkout.md). After changing source, restart API:

```sh
docker compose -p cinema-be2 -f docker-compose.be2.yml restart api
```

Bootstrap POST `/api/guest-context` first and keep its cookie. Copy the returned
`X-CSRF-Token` into selection write requests, with these headers:

```text
Origin: http://127.0.0.1:5173
X-CSRF-Token: <bootstrap response header>
Idempotency-Key: {{$guid}}
Content-Type: application/json
```

`Idempotency-Key` is required for POST, not PUT. For replay testing use one fixed
UUID: Postman's dynamic `{{$guid}}` generates a new one on each request.

POST `/api/selections` example (replace IDs with real local database values for
an eligible scheduled showtime and seats in its room):

```json
{
  "showtimeId": "123",
  "quantities": { "adult": 1, "discounted": 1 },
  "seatIds": ["456"]
}
```

The example represents two tickets with only one seat chosen so far, which is
valid during editing. Creation needs at least one physical seat. GET returns
`data.selectionId`, quantities, seat IDs, quote, `selectionDeadline`, `serverTime`,
state, bookingId (normally null before task 3), and version. Save the returned ID
and version. To edit, PUT `/api/selections/<selectionId>`:

```json
{
  "version": "1",
  "quantities": { "adult": 1, "discounted": 1 },
  "seatIds": ["456", "457"]
}
```

Use the actual latest version and seat IDs. Empty seat arrays are permitted on
PUT while editing; they never restart the timer. Reserve in task 3 must require
exactly as many distinct seats as tickets.

## Lifecycle, pricing and retry

- Five minutes are stored using database time and the SQL-mandated 300-second
  duration. Reload, edit, quote refresh, deselect/reselect and retry do not extend
  the deadline. Guest lifetime remains separately configured at 24 hours.
- `Selection:MaximumTickets` defaults to 8 and must be positive. The limit is
  snapshotted on creation so existing selection limits do not change mid-checkout.
- One active selection per Guest/showtime is recovered by a new create request;
  incoming edits are not applied by create-reuse. Use PUT to change it.
- Create fingerprints canonicalize numeric IDs and sort seat sets. Same key and
  payload returns the existing selection with `Idempotency-Replayed: true`;
  different payload returns 409 `IDEMPOTENCY_KEY_CONFLICT`. Actor-level advisory
  locking also serializes creates with different keys to protect the active index.
- An expired active row is marked expired before creating a replacement. Replaying
  an old key returns that old expired selection, never the replacement or a fresh
  deadline. GET returns expired state; PUT returns 422 `SELECTION_EXPIRED` and
  persists expiry. Consumed/abandoned selections cannot be edited.
- Different Guests may choose the same physical seat. Selection writes only
  `selection_session`, `selection_seat` and its operation receipt. Reservation
  later decides exclusive allocation.
- Quotation reads Adult/Discounted prices from the cinema and adds each selected
  seat's surcharge. Missing prices are a business error, never assumed to be zero.
  Incomplete selections quote all requested category quantities plus surcharges
  for the seats currently selected; reserve eligibility remains separate.
- A quote fingerprint covers selection identity, quantities, price values and
  versions, current official start, seat IDs/labels/type names/surcharges. A stable
  UUID is derived from that fingerprint. The persisted fingerprint and financial
  fields support task 3 revalidation without an extra schema table.
- GET refreshes active quotations against current database values; price/start/
  seat changes alter quote version/ID and bump selection version. PUT requires
  the latest selection version; stale requests return 409 `VERSION_CONFLICT` with
  `details.currentVersion`. Quotes have a current `quotedAt`, without extending
  selection expiry. Expired/consumed reads do not reprice historical bookings.
- Wrong ownership is 404. Invalid/duplicate/wrong-room/inactive/retired seats and
  invalid quantities are 400 validation errors. Unbookable showtimes return 422
  `SHOWTIME_NOT_BOOKABLE`; missing pricing returns 422 `BUSINESS_RULE_VIOLATION`.

## Verification

```sh
python3 scripts/test-guest-context.py
```

The existing runner discovers both Guest and Selection test classes in the same
project, using disposable PostgreSQL 16.4 and .NET 8 SDK containers. Selection
fixtures obtain IDs from the database and move a fixture showtime into the future
using database time; production time checks and the seed scripts are unchanged.
Tests clean up their own selections so Guest regression tests remain independent.
No shared Supabase, gateway or real customer data is used. The runner publishes
no ports and cleans up its own containers/network.

The test report remains `tests/CinemaTicketBooking.Api.Tests/TestResults/guest-context.trx`
(Git-ignored). Task 3 reservation, seat races, and payment were not implemented or
verified by this change.

Task 3 reservation is now implemented on `feat/be-booking-reservation`: [booking-reservation.md](booking-reservation.md). The branch reuses the parameterized read adapter and shared ancestor/selection locking, while keeping BE1 mappings and endpoints unchanged. Quote fingerprints now also bind showtime identity/version and movie/room metadata for reservation reacceptance.
