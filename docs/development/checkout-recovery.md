# Booking reads and checkout recovery — BE2 task 4

Branch: `feat/be-checkout-recovery`, based on completed task 3 at `34634f0`.
No new API keys, services, schema changes or migrations are required. Use the
existing PostgreSQL connection and Guest-cookie configuration. No MoMo/Brevo
account is needed for this task. The backend does not seed or migrate on startup.

## Run and test locally

The Arch Docker API copies source when it starts. With the existing local stack
running, reload it after switching to this branch:

```sh
export CINEMA_BE2_DATA_DIR="$HOME/.local/share/cinema-be2/postgres-16"
docker compose -p cinema-be2 -f docker-compose.be2.yml restart api
```

Keep your existing data-directory value if you configured a different location.
For first startup see [Guest checkout](guest-checkout.md). API base URL remains
`http://127.0.0.1:5080`.

Re-import [Cinema-BE2.postman_collection.json](../api/postman/Cinema-BE2.postman_collection.json),
revision **1.4.0**, replacing the older collection. Select **Cinema BE2 Local**;
keep your configured environment or import the blank template for a fresh setup.
Use real available seats and run **01–19** in order. Create/reserve within the
original five-minute deadline. Requests **15–17** read and reload; **18** expects
401 with its cookie jar disabled, and **19** expects 404 for a missing booking.
No new manual variables are required: request 11 captures `bookingId` and
`bookingVersion`. See [Postman setup](../api/postman/README.md).

## Endpoints and authority

```http
GET /api/bookings/{bookingId}
GET /api/bookings/{bookingId}/checkout
```

Both require the valid owning `cinema_guest` cookie. No request body, CSRF token,
Origin or idempotency key is required for GET. Responses and errors are
`Cache-Control: no-store`. Unsupported Authorization still returns 401 with no
Guest fallback. Missing/invalid/expired/revoked Guest proof returns 401; an absent
booking or another Guest's booking returns the same 404 `NOT_FOUND`. IDs must be
positive bigint decimal strings; malformed/out-of-range IDs return 400
`VALIDATION_ERROR`. Neither a booking code nor an email/phone grants access.

The first route returns `{ "data": Booking }`. The checkout route returns:

```json
{
  "data": {
    "booking": { "bookingId": "<decimal string>", "status": "pending" },
    "payment": null,
    "nextAction": "pay",
    "serverTime": "<UTC ISO 8601>"
  }
}
```

The example abbreviates Booking; the actual response includes committed contact,
quantities/admissions, physical-seat and financial snapshots, original deadline,
version and current official showtime. `purchasedStartAt` remains the purchase-time
snapshot. Current official times, names, status and sale eligibility come from
the current showtime/venue data. Price configuration changes never reprice an
existing booking. Both routes use the same projection as reservation replay.

## Recovery decisions

`nextAction` is a navigation hint, never payment/admission authority. The following
rules are evaluated in order:

| Current evidence | nextAction |
|---|---|
| Payment needs review/refund, or an approved cancellation blocks admission | `contactSupport` |
| Created/pending/unknown payment, or succeeded but not applied | `awaitOutcome` for pending booking; `contactSupport` for inconsistent terminal booking |
| Persisted booking expired/cancelled without unresolved financial evidence | `selectAgain` |
| Confirmed booking with an existing valid ticket | `viewTicket` |
| Confirmed booking without a valid ticket | `contactSupport` |
| Any other existing payment attempt on a pending booking | `contactSupport`; no payment retry policy is invented here |
| Pending with no payment, original deadline elapsed or showtime ineligible | `selectAgain` |
| Pending with missing/released/sold allocations, or zero total | `contactSupport` |
| Eligible positive-total pending booking, complete held seats and time remaining | `pay` |

The Booking status remains its **persisted** status. In particular, a pending
booking past its unpaid deadline remains `pending` until task 5 processes it;
`nextAction=selectAgain` and the elapsed deadline mean FE must not show it as an
eligible hold or offer Pay. A GET never marks it expired, releases seats or claims
those seats are available. An unresolved payment continues to return
`awaitOutcome` even past its payment deadline/grace, without assuming failure.

If payment rows already exist, recovery returns the relevant protected Payment
summary and its original deadline. Review/refund obligations take precedence,
then unapplied success, open attempt, applied attempt, and finally latest terminal
attempt. Payment IDs and booking IDs are decimal strings. Raw gateway evidence,
merchant/provider transaction identities, credentials and worker leases are not
returned. An existing open rescheduling cancellation is projected as the minimal
CancellationSummary, without its private reason or reviewer details.

FE2 should label an eligible pending booking **Chờ thanh toán** and display MoMo
as the intended demo method. Choosing MoMo does not call a payment endpoint or
confirm a booking. `pay` means FE may show that step; gateway initiation remains
outside this demo. `viewTicket` only references an existing ticket: these GETs do
not issue one or disclose its QR. Missing Guest cookies require the existing
support route; no public lookup or credential-recovery bypass is added.

## Consistency and verification

Reads use a fresh EF context inside a repeatable-read transaction, with the
existing actor/Guest → Cinema → Room → Showtime → Booking lock order. All child
snapshots come from that transaction, with existing bounded serialization/deadlock
retries. Guest validity is checked again against database time before returning.
There is no SaveChanges, deadline refresh, receipt/audit insertion or external call.

```sh
python3 scripts/test-guest-context.py
```

The runner uses .NET 8 SDK and PostgreSQL 16.4 containers with temporary storage
and generated credentials; it never uses the local demo or shared Supabase DB.
Recovery tests share the reservation harness in `BookingRecoveryTests.cs` and
cover both routes, ownership, credentials, IDs, immutable reloads, current versus
purchased time, terminal/elapsed states, unresolved payments, review, ticket and
cancellation projections, zero totals and concurrent committed state changes.
Payment/ticket/reviewer rows are test-only fixtures; this task implements no such
write workflow. Task 5 cancellation/expiry, gateway calls, email, ticket issuance,
online deployment and network/uncertain-commit fault injection remain outside scope.

Verified on 2026-11-12: **95/95 integration cases passed, 0 skipped**, including
the 60 earlier regression cases and 35 recovery cases. Live Kestrel/Newman 6:
**19/19 requests and 67/67 assertions passed**. Database checks found exactly one
booking and one held seat, with zero payment, ticket or notification rows after
the Postman flow. Test containers, networks and generated credentials were cleaned
up. The user's existing local API/database was not restarted or used for these checks.
