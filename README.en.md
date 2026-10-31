> **v2 Customer Portal:** Added guest sign-up/sign-in, equipment and lodging reservations, activity enrollment, booking history, payments and profile pages. Includes additive demo content when `SEED_DEMO_DATA=true`. Preserve the existing Docker Compose data volume and `.env` when upgrading. Read [customer-portal.md](docs/customer-portal.md) for full upgrade/security details. Not production-complete.

# AlpineOS — Ski Resort Management System

An original, full-stack redesign of a university-era **Ski Resort Automation** database assignment, preserving its core business purpose while introducing a modern web-based operations dashboard.

**Status:** Portfolio-oriented local-development project. The author has confirmed it runs with Docker on their computer; automated GitHub CI still needs independent verification after publication. **Not approved for production or real payments.**

## Modules

| Module | Included features |
|---|---|
| Authentication | Staff login, password hashing, role policies, anti-CSRF, login lockout, audit trail |
| Customers | Customer profiles and contact information |
| Inventory | Unique equipment assets, daily rates, maintenance and availability status |
| Rentals | Reservation, issue, return, cancellation, automatic pricing, DB overlap prevention |
| Accommodation | Rooms, capacity checks, stay bookings, check-in/out, DB overlap prevention |
| Resort operations | Slope status, snow conditions and weather readings |
| Activities | Lessons, competitions, events, instructor assignment, capacity-based enrollment |
| Finance | Manual charge and refund ledger with balance checks; **no payment processing** |
| Analytics | Operational KPIs, recent transactions, revenue graph and CSV export |

## Stack

ASP.NET Core 10 / C# · EF Core / Npgsql 10 · PostgreSQL 17 · React 19 · TypeScript · Vite · Nginx · Docker Compose · xUnit · GitHub Actions.

## Run locally

1. Install Docker Desktop with Docker Compose.
2. Extract the ZIP and open a terminal in the root folder.
3. Copy `.env.example` to `.env`.
4. Change `POSTGRES_PASSWORD` and `ADMIN_PASSWORD` to different long random strings, and adjust `ADMIN_EMAIL`.
5. Run:

```bash
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 api
```

6. Open **http://localhost:8088**, then log in with the administrator credentials from `.env`.

All external bindings are limited to `127.0.0.1`. The database and API are not published as host ports. To stop (while retaining database data), run `docker compose down`. Do not use `docker compose down -v` with data you need to preserve.

To run unit tests with .NET 10 SDK:

```bash
dotnet test tests/SkiResort.Tests/SkiResort.Tests.csproj
```

To build the frontend with Node.js 22+:

```bash
cd web
npm install
npm run build
```

For HTTP smoke tests, start the stack and provide `ADMIN_EMAIL` and `ADMIN_PASSWORD` as process environment variables, then run `python scripts/smoke_test.py`. State-changing integration scenarios are in `scripts/integration_scenarios.py` and **must run against disposable test data only**.

## Architecture and security

- The UI and API are same-origin through Nginx; cookie authentication uses HttpOnly and SameSite Strict. Production configuration requires HTTPS and Secure cookies.
- Mutating `/api` endpoints require an anti-CSRF header.
- Database exclusion constraints prevent overlapping active reservations for the same equipment asset or room, including concurrent requests.
- Pricing uses fixed server-side rate snapshots, not caller-supplied totals.
- `AuditEvents` tracks important operational actions.
- Authorized **write** permissions are role-scoped; some read access is currently shared by all authenticated staff. Restrict further before real customer data is used.

## Known limitations

The initial development database uses EF `EnsureCreatedAsync`, **not a managed migration chain**. Production upgrades require creating and validating an EF migration baseline. Full integration/E2E/load testing, TLS hosting, GDPR/KVKK governance, real payment gateway integration, fiscal invoicing, backup restore rehearsal, multi-tenant isolation, and a customer self-service portal are not included.

The original uploaded archive included an Access database, reports, and two different relationship diagrams, but no Visual Studio application source files. The new implementation is a **later modernization**, not a verbatim migration of the original code. The original `.accdb` is deliberately omitted to avoid accidentally publishing potentially private data.

Full documentation: [`README.md`](README.md), [`docs/architecture.md`](docs/architecture.md), [`docs/testing.md`](docs/testing.md), [`docs/api-reference.md`](docs/api-reference.md), [`docs/legacy-audit.md`](docs/legacy-audit.md).

MIT License — see [`LICENSE`](LICENSE).
