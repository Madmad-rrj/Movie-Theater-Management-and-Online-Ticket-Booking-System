# Pending cancellation and expiry — BE2 task 5

Branch `feat/be-booking-cancel-expiry` includes task 4 at `fa81f82`. No new API
keys, gateway account, external scheduler, database migration or schema change is
required. The expiry worker runs inside the API process and uses the existing
PostgreSQL connection. It does not call MoMo, Brevo or any other external service.

## Local Arch setup

The local Docker profile enables expiry every **15 seconds**, with up to **50
candidates per sweep**. Existing `.env` files work with these defaults. Start or
recreate the API to load the source and optional Compose settings:

```sh
export CINEMA_BE2_DATA_DIR="$HOME/.local/share/cinema-be2/postgres-16"
docker compose -p cinema-be2 -f docker-compose.be2.yml up -d --no-deps --force-recreate --wait api
```

Keep your existing data-directory value if different. This assumes PostgreSQL is
already running; first startup remains in [Guest checkout](guest-checkout.md).
Starting this API enables cleanup of existing eligible overdue unpaid pending
bookings in that local DB. Protected payments and confirmed bookings remain held
or sold. Neither startup nor the worker resets or seeds a database.

Optional local `.env` overrides, also listed in `.env.example`:

| Variable | Default | Meaning |
|---|---|---|
| `CINEMA_BE2_EXPIRY_ENABLED` | `true` | Run the background worker in the local Compose profile. |
| `CINEMA_BE2_EXPIRY_POLL_SECONDS` | `15` | Delay after each sweep, 1–3600 seconds. |
| `CINEMA_BE2_EXPIRY_BATCH_SIZE` | `50` | Candidates per sweep, 1–500. |

Recreate the API after changing Compose environment values. Native/deployed API
configuration uses `BookingExpiry__Enabled`, `BookingExpiry__PollIntervalSeconds`
and `BookingExpiry__BatchSize` instead. Development JSON explicitly enables the
worker; outside Development it defaults to disabled until explicitly enabled.
There is no public HTTP worker endpoint. Keep the API running for expiry to occur.

## Explicit pending cancellation

```http
POST /api/bookings/{bookingId}/cancel
Origin: http://127.0.0.1:5173
X-CSRF-Token: <current Guest CSRF token>
Idempotency-Key: <UUID for this cancellation intent>
Content-Type: application/json

{"confirmed": true}
```

Retain the owning `cinema_guest` cookie. Return: **200 `{ "data": Booking }`**
with `status=cancelled`, version advanced once, original deadline, contact and all
financial/physical-seat snapshots preserved. Backend stores cancellation time and
the fixed reason `guest_cancelled_before_payment`; it does not accept arbitrary
reasons, actor IDs, status, amounts or allocation changes from the client.

Cancellation requires a currently pending booking with complete held allocations,
zero admissions, no committed payment attempt and no ticket/refund/open
rescheduling obligation. Any payment row crosses the initiation-commit boundary,
including created/unknown attempts without an initiation timestamp or redirect.
Failed/cancelled historical attempts do not reopen ordinary cancellation. A still
pending unpaid booking can be explicitly cancelled after its original deadline
if the worker has not already expired it; the transaction resolves that race.

One transaction changes pending→cancelled, every held allocation→released,
`released_at` using database time, version, audit and operation receipt. Failure or
Guest expiry before commit rolls back all of these. The selection stays consumed;
choosing seats again requires a new selection. Available seats may be reserved
again only after the release commit and normal showtime/seat checks.

Idempotency scope is actor + `cancelPending` + canonical booking target. Same key
and valid payload returns the same booking outcome with `Idempotency-Replayed:
true`, with no second version increment/release/audit. The key may be reused for
a different booking target. Sanitized 422 outcomes are durable and replay-marked
as well; an altered fingerprint conflicts with 409. Invalid bodies fail validation
before receipt processing. Repeating with a fresh key after cancellation returns
422 rather than pretending another cancellation happened.

| Result | Meaning |
|---|---|
| 400 `VALIDATION_ERROR` | Invalid ID/key/body, missing or false confirmation, unknown write fields. |
| 401 `UNAUTHENTICATED` | Missing/invalid/expired/revoked Guest proof. |
| 403 `CSRF_REJECTED` | Missing/invalid CSRF or untrusted Origin. |
| 404 `NOT_FOUND` | Missing booking or another Guest's booking. |
| 409 `IDEMPOTENCY_KEY_CONFLICT` / `OPERATION_IN_PROGRESS` | Receipt conflict or unresolved operation. |
| 422 `CANCELLATION_NOT_ELIGIBLE` | `reason`: `booking_not_pending`, `payment_initiation_committed`, `allocation_state_inconsistent` or `protected_obligation`. |

All protected responses are no-store. Back/tab-close or a countdown reaching zero
does not submit cancellation. FE2 must show the seat-release warning and send the
explicit confirmed request only when the customer accepts it.

## Safe expiry and payment boundaries

The worker scans pending bookings past the original reservation deadline. Discovery
is only a candidate scan; each booking gets a fresh transaction and rechecks state,
database time, every payment attempt and allocations under the shared lock order:
actor advisory → Guest row → Cinema → Room → Showtime → Booking → Payments.
Cleanup does not require the Guest to remain valid or the browser to remain open.

NOWAIT/transaction-scoped row claims skip busy resources. Lock conflicts are handled
inside the EF retry strategy, so they do not become retry-limit errors. Existing
deadlock/serialization retries remain available for actual transaction races. A
cursor continues past protected candidates and wraps at the end; with a large
backlog, cleanup can take multiple sweeps. Multi-instance workers recheck under the
same locks, so only one can perform the pending→expired transition.

Release requires complete held allocations and zero admissions. The worker also
protects existing tickets, refunds and open rescheduling obligations. It defers if
any payment is open/unknown/created/pending, succeeded, applied, unverified,
requires review/refund, reports collected funds, remains within a payment window
or grace, or has an associated unprocessed verified payment event. Passing a
deadline or grace is never evidence of failure.

Only definitive **verified failed/cancelled** attempts, without collected funds,
application, unresolved reconciliation or pending events, and past any established
payment window/grace, can permit expiry. Every attempt must be safe; a newer
failure does not hide an older unresolved payment. No gateway verification or
reconciliation is invented here. A protected booking remains pending until the
future payment/reconciliation workflow establishes safe evidence; logs record the
deferred count. No financial status is changed by this worker.

Eligible expiry atomically changes pending→expired, increments version once,
releases all held seats with one database timestamp and writes one audit event.
Original deadlines, selection, booking categories/contact/prices and payment
history remain intact. Sold/mixed/missing allocations are deferred for review.
Repeated runs preserve the original release timestamp and do not reclaim seats
after late payment success. Confirmed, cancelled and expired bookings are skipped.

A failure rolls back that booking and does not stop processing other candidates;
later sweeps retry it. Aggregate logs report scanned/expired/deferred/busy/skipped/
failed counts without contact, cookies, tokens, raw SQL exceptions or connection
strings. Shutdown cancellation stops polling and propagates to database work.

## Postman and automated tests

Re-import [the collection](../api/postman/Cinema-BE2.postman_collection.json), revision
**1.5.0**, replacing the old copy. Keep **Cinema BE2 Local** selected and configured
with real available local IDs. No new required manual variables are added.

- **01–26 Main flow**: reserve/recover, reject missing confirmation/CSRF, cancel,
  replay, recover cancelled state, and replay a rejected new cancellation intent.
  This ends with one cancelled booking and released seats.
- **27–30 Optional natural expiry**: create a new selection on those released
  seats, reserve again, poll until persisted expiry, then read expired history.
  Allow about **five minutes plus the worker interval**. Collection Runner/Newman
  loops request 29 once per second; with Send, revisit request 29 after the deadline
  and worker sweep. `expiryWaitSeconds` defaults to 420 and bounds polling. Increase
  it for a deliberately longer poll interval/backlog. No Postman request alters
  server clocks, database times or invokes a worker trigger.

Running the whole collection includes that wait; select only the main folder for
the fast path. Optional-flow variables replace the current selection/booking IDs;
`cancelledBookingId` retains the first booking's reference. Keep cookies enabled
apart from deliberately negative request 18. Do not export populated environments
or cookie jars. [Full import guide](../api/postman/README.md).

```sh
python3 scripts/test-guest-context.py
```

The runner uses disposable .NET 8 SDK/PostgreSQL 16.4 containers; it never runs
mutating fixtures against shared Supabase or the user's local demo. Most tests
disable automatic polling and invoke the internal service deterministically; a
separate hosted-worker test verifies cleanup without a browser request. Tests use
controlled payment/ticket fixtures and failure/delay triggers only inside that
temporary DB. No payment, ticket issuance, email, refund execution, online
deployment or network/uncertain-commit fault injection is implemented here.

Verified across 2026-11-12–13: **134/134 PostgreSQL integration tests passed, 0
skipped** (95 earlier cases plus 39 task-5 cases). Final tests include cancellation
ownership/confirmation/CSRF/key checks, success/rejection replay, payment-state
guards, all-seat rollback, Guest expiry during writes, verified terminal outcomes,
payment windows/grace/events/review, terminal and inconsistent allocations,
multi-worker/cancellation/initiation races, fair bounded sweeps, per-booking
failure isolation/retry and hosted cleanup without browser requests.

Newman 6 ran all **30 defined Postman requests**, including natural expiry without
clock/fixture deadline changes: **337 HTTP executions** due to polling and
**1,033 assertions**, all passed, in **5 minutes 15 seconds**. SQL checks verified
one cancelled booking and one expired booking, zero held seats, and zero payments,
tickets or notifications. All test containers/networks/generated credentials were
cleaned up. The user's existing demo stack was not restarted or used for verification.
