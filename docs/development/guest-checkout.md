# Guest checkout — BE2 task 1

Implemented on `feat/be-guest-context`, starting from `6f412ae`. This branch provides
`POST /api/guest-context` and the shared Guest authorization/CSRF mechanism. It does
not implement selection, booking, payment, or any BE1 discovery/pricing endpoint.
The SQL schema is unchanged and is never bootstrapped by API startup.

## Local setup

### Run the backend and database entirely in Docker on Linux/Arch

From the repository root:

```sh
[ -f .env ] || cp .env.example .env
export CINEMA_BE2_DATA_DIR="$HOME/.local/share/cinema-be2/postgres-16"
mkdir -p "$CINEMA_BE2_DATA_DIR"
docker compose -p cinema-be2 -f docker-compose.be2.yml up -d --wait
```

Use a directory on a Linux filesystem that supports Unix permissions. On the
reported Arch environment, Docker's data root is on `/mnt/D` (`fuseblk`), and the
original named-volume PostgreSQL startup failed to chmod its data directory.
The Linux home is on ext4; this profile stores PostgreSQL there using a bind mount.
It does not reset or reuse the failed demo volume. Data persists across container
restarts in `CINEMA_BE2_DATA_DIR`.

The API runs at `http://127.0.0.1:5080`, using the PostgreSQL settings in root
`.env` (default local DB port 5433). It uses the SDK image's existing `app` user;
passing an undefined numeric user reproduced the `NuGet-Migrations` mutex error
on this machine. API source is mounted read-only, copied into temporary Linux
storage, and built inside the container. No host .NET SDK or frontend is needed
for Postman. Source changes require restarting the API container.

```sh
docker compose -p cinema-be2 -f docker-compose.be2.yml logs -f api
docker compose -p cinema-be2 -f docker-compose.be2.yml restart api
```

For Postman, POST `http://127.0.0.1:5080/api/guest-context` without a body, with
`Origin: http://127.0.0.1:5173` and `Idempotency-Key: {{$guid}}`. Retain cookies for
reuse. Do not put the Guest cookie or CSRF token in shared logs.

Verified on 2026-11-11 with this profile: both services healthy, live Guest
create/replay/reuse returned 201/200/200 with unchanged identity/deadline, a CSRF
header and replay marker; movie listing returned six seeded movies. This was a
local smoke check, not a new run of the full integration suite.

### Run the backend with a host .NET SDK

Use the existing local PostgreSQL setup in [local-development-setup.md](local-development-setup.md).
The backend targets .NET 8. Set `ConnectionStrings__DefaultConnection` in the API
process environment; root `.env` is used by Compose and is not automatically read
by the API. Use a local development database, not shared Supabase, for write tests.

Run the API at `http://127.0.0.1:5080` with `ASPNETCORE_ENVIRONMENT=Development`,
then run the existing Vite frontend at `http://127.0.0.1:5173`.
Browser requests use relative `/api` paths. Keep `changeOrigin: false` in the Vite
proxy so the backend sees the browser's actual Origin.

`appsettings.Development.json` supplies these bound options:

| Option under `GuestCheckout` | Demo value | Meaning |
|---|---|---|
| `LifetimeHours` | `24` | Agreed demo lifetime, absolute from database creation time; reuse does not extend it. Valid configuration range: 1–720 hours. |
| `AllowedOrigins` | `["http://127.0.0.1:5173"]` | Exact trusted browser origins, including scheme and port; no wildcard or trailing slash. |
| `AllowInsecureLoopbackCookies` | `true` | Development only: HTTP requires both a loopback request host and a loopback peer address. |
| `CrossSiteCookies` | `false` | Lax cookies for local same-site operation; `true` selects SameSite=None and requires HTTPS. |
| `BootstrapPermitLimit` | `20` | Technical default: bootstrap requests per fixed window, partitioned by direct peer IP. |
| `BootstrapWindowSeconds` | `60` | Window length; these are configured limits, not measured capacity results. |

The cookie is host-only, HttpOnly, scoped to `/api`, and has a fixed Expires value.
HTTPS always uses Secure cookies. The HTTP exception cannot be enabled outside
Development. The API does not trust arbitrary forwarded-IP/scheme headers.

## FE2 bootstrap and reuse

```ts
const response = await fetch('/api/guest-context', {
  method: 'POST',
  credentials: 'include',
  headers: { 'Idempotency-Key': crypto.randomUUID() },
});

if (!response.ok) {
  // Handle the contract error before proceeding with checkout.
  throw new Error('Guest context unavailable');
}

const { data: guest } = await response.json();
const csrfToken = response.headers.get('X-CSRF-Token');
// Keep the CSRF token in memory. Never log it or store the Guest cookie in JS.
```

There is no request body. A new context returns 201; reuse and replay return 200.
The only JSON fields inside `data` are `contextId` (UUID) and `expiresAt` (UTC with
`Z`). `Set-Cookie` carries the credential and `X-CSRF-Token` carries the CSRF token.
Responses use `Cache-Control: no-store`. A recognized replay also returns
`Idempotency-Replayed: true`.

The server generates a 256-bit random credential and stores only its SHA-256 hash
in `checkout_identity`. It uses database time for creation and validity checks.
Rebootstrap with a valid cookie preserves the same identity, credential, and
expiry. This also recovers the CSRF header after a reload.

The CSRF token is HMAC-SHA-256 of a versioned, context-specific label using the
Guest credential as key. Verification uses a constant-time comparison. It needs
no additional shared secret or CSRF database column. Other Guest mutations must
send both the cookie and `X-CSRF-Token`, and must have a trusted Origin.
Bootstrap is the initial-token exception: it validates Origin but does not require
an existing CSRF token, does not modify selection/booking state, and never renews
an existing Guest's deadline.

Use one idempotency key for retries of the same bootstrap operation. The ledger
stores only operation metadata and a context reference, never a recoverable
credential or CSRF token. Advisory locking prevents concurrent duplicate creation.
An in-process database retry uses the original generated credential to recover an
uncertain commit. An HTTP replay must still prove access with its matching cookie.

If the entire first response, including Set-Cookie, was lost, the key alone cannot
recover that credential: replay returns 401. The client can begin a fresh bootstrap
with a new key; no selections or bookings could have been created through the
undelivered credential. If the cookie for an existing checkout is lost, do not
promise recovery by name, email, phone, public context ID, or booking code.

For a malformed, unknown, expired, or revoked cookie, the server returns 401 and
expires the invalid cookie. FE must report loss of the prior checkout context
before deliberately starting another one. Supplying an unsupported Authorization
header returns 401 and does not silently fall back to or clear a valid Guest cookie.

## Error outcomes

All application errors contain `error.code`, `message`, `details` (object), and
`traceId`. Secrets, SQL, stack traces, and provider errors are not response fields.

| HTTP | Code | Relevant condition |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Missing/malformed/multiple/zero UUID idempotency key, or a request body; `details.violations` identifies the field. |
| 401 | `UNAUTHENTICATED` | Invalid/expired/revoked Guest, missing proof on replay, or unsupported Authorization header. |
| 403 | `CSRF_REJECTED` | Missing/untrusted Origin, or absent/invalid/cross-context CSRF token on a protected mutation. |
| 403 | `FORBIDDEN` | Insecure transport outside the explicit local exception. |
| 409 | `IDEMPOTENCY_KEY_CONFLICT` | Key belongs to a different Guest or incompatible recorded request. |
| 409 | `OPERATION_IN_PROGRESS` | Concurrent operation owns the bootstrap lock; `Retry-After: 1`. |
| 429 | `RATE_LIMITED` | Bootstrap limit reached; `Retry-After` supplied. |
| 503 | `DEPENDENCY_UNAVAILABLE` | Transient database failure after bounded retries. |
| 500 | `INTERNAL_ERROR` | Unexpected error; use the trace ID for investigation. |

## Reuse in subsequent BE2 branches

Apply `[GuestCheckout]` to the Guest-only controller or action. The filter resolves
the current credential against database expiry/revocation, rejects unsupported
Authorization headers, checks mutation Origin/CSRF, sets no-store, and places the
validated `GuestContextDto` in `HttpContext.Features`.

This establishes caller identity. Owning services must still compare selection or
booking ownership, and revalidate current identity/time inside their protected
transaction before committing. A DTO's contextId is not independent proof of access.
Integrate Customer authorization when that component exists; this branch does not
implement login or silently treat bearer-authenticated requests as Guests.

BE1's shared cinema/room/showtime/seat/pricing models are untouched. BE2 task 2
requires integrating those mappings before quotation/reservation implementation.
The existing numeric-ID/UUID mismatch for those future DTOs remains to be aligned;
Guest and selection identity types are UUIDs in the current SQL baseline.

## Isolated verification

With Python 3 and a working Docker daemon in Linux-container mode:

```sh
python3 scripts/test-guest-context.py
```

The runner uses the official .NET 8 SDK and PostgreSQL 16.4 images, a random private
Docker network, generated test-only credentials, and temporary PostgreSQL storage.
It publishes no host ports, reads no root `.env`, and never connects to Supabase or
the existing demo database. It copies source to the SDK container and applies the
unchanged SQL bootstrap and seed only through the test fixture. Normal completion
removes that invocation's containers/network and copies the TRX report to the
Git-ignored `tests/CinemaTicketBooking.Api.Tests/TestResults/` directory.

For an already prepared .NET SDK environment, `dotnet test` also works when
`CINEMA_TEST_CONNECTION` points to a new local database named `cinema_be2_test_*`.
The fixture refuses cloud hosts, unrelated database names, or an existing `cinema`
namespace. Each run needs a fresh test database; it does not drop/reset one.

The suite uses the real PostgreSQL schema and HTTP test host. Probe endpoints that
exercise `[GuestCheckout]` exist only in the test assembly and are not shipped.
Coverage includes lifecycle, hash-only storage, cookie flags, origin/CSRF rejection,
bearer precedence, concurrent bootstrap, idempotency, rate limiting, unsafe online
configuration, and the existing PostgreSQL-backed movie-list endpoint.

This is Guest-context verification, not a claim that selection, seat reservations,
online hosting, MoMo, or email integration has been tested. Database-disconnection
and uncertain-commit fault injection were not performed.

Verification on 2026-11-11: API build completed with zero warnings/errors;
26 PostgreSQL/HTTP integration cases passed with zero skipped. A real Chromium
check through the existing Vite proxy (`127.0.0.1:5173` → API `127.0.0.1:5080`)
received 201/200/200 for create/replay/reuse, retained the same Guest and deadline,
read the CSRF header and replay marker, and still retrieved six seeded movies.
The browser harness was temporary and did not modify frontend source.

Technical references: [Microsoft CSRF guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)
and [EF Core retry/transaction guidance](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).
