# Local Mac setup

## Prerequisites

Install Node.js 22 LTS (22.22.3+) from https://nodejs.org/en/download and .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0. Choose Arm64 on Apple Silicon. Check `node --version`, `npm --version` and `dotnet --info` in Terminal.

For Git, run `git --version`; if unavailable run `xcode-select --install` and complete Apple's installer. See [Git instructions](GITHUB.md).

```sh
git clone https://github.com/reachus-sreeni/service-connect.git
cd service-connect
dotnet run --project backend/ServiceConnect.Api
```

The Local launch profile sets Development and localhost:5000. Default local settings select SQLite, apply migrations, create a stable local JWT key, and seed synthetic accounts and jobs if the account store is empty. This works on Apple Silicon without Docker or a remote SQL Server.

In a second Terminal, from the repository root:

```sh
cd frontend
npm ci
npm start
```

Visit http://localhost:4200. The frontend proxies `/api` to the local API. Check http://localhost:5000/health for process health.

## Where local data goes

Relative to the repository root:

- `backend/ServiceConnect.Api/.local/service-connect.db`: SQLite database, including accounts, password hashes, verification state, providers, services, requests, quotes, reviews and activity.
- `backend/ServiceConnect.Api/.local/outbox/`: JSON test messages with channel, destination, challenge ID and verification code. No real messages are sent in LocalOutbox mode.
- `backend/ServiceConnect.Api/.local/jwt.key`: generated local signing key.

These files are private development data, excluded from Git and Docker. Keep this directory on your machine. SQLite data persists across API restarts. The outbox is restricted to Development; Production defaults to external delivery. Old outbox files can remain, but codes expire after ten minutes and only the current unused challenge is valid. Do not share screenshots/files containing real verification codes or keys.

To view codes while testing, open the folder:

```sh
open backend/ServiceConnect.Api/.local/outbox
```

Open the newest JSON matching the account/channel you just requested. Enter its six-digit `code` in the application. It is never returned by a normal API response.

## Suggested manual tests

All seeded users use `Local-Demo-Only-2026!`. See the README for accounts.

1. Sign in as `alex@example.test`. Inspect the four sample jobs, existing estimates, revisions, history and completed feedback.
2. Use a private window for `morgan@example.test`. For the accepted sample job, choose **Mark work done**. The job becomes AwaitingCustomerReview.
3. Back as Alex, refresh. Confirm completion with all 1–5 ratings and written feedback. Completion without feedback must fail. The job then becomes Completed.
4. As Morgan, refresh and rate the customer's communication, cooperation and readiness. Repeated feedback is rejected.
5. Sign in as `casey@example.test`. Request email and phone codes, read each local outbox file and verify both contacts. Casey still cannot quote until approved.
6. Sign in as `admin@example.test`; review Casey's qualifications, enter review notes and approve. Refresh Casey's screen and submit an estimate to an open request.
7. Register a new Customer or Provider. Email and international phone are required. New contacts begin unverified. New providers also need business name/qualifications and begin Pending.
8. Try `jamie@example.test` against Alex's jobs: Jamie should not see Alex's private requests or activity. Competing providers must not see each other's estimates.
9. Stop/restart the API. Confirm your account changes, job states, quotes and reviews remain.

Phone numbers must be in international format, for example `+12025550123`; formatting characters and spaces are rejected. Demo contact details are synthetic. Contact information is not editable in this version; adding contact changes will require clearing verification and re-verifying the new destination.

## Resetting test data

Stop the API first. Back up/move the entire `.local` folder outside the project if you want to retain it, then restart with the folder absent. A fresh database/key/demo dataset will be created. This is an intentional manual reset; the app never resets your test data automatically. Do not use these reset steps on a real database.

## Configuration and troubleshooting

- `Database__Provider=Sqlite` selects the file database; `ConnectionStrings__Sqlite` can override its path. The default is based on the API project content root.
- Set `Database__SeedDemo=false` before a fresh start to disable demo accounts. Changing this after seeding does not delete them.
- Use `Demo__Password` before the first seed to choose a different shared demo password; changing it later does not reset accounts.
- The development launch profile disables JSON configuration file watchers to avoid the Mac environment's watcher restriction. Restart after configuration changes.
- Angular's disk cache is disabled because its native cache library crashed on the development Mac.
- Authentication and verification endpoints are rate limited. Codes have a 60-second resend cooldown and a five-per-contact-per-hour limit. Wait when a limit is reached.
- A 409 means data changed or an action is not valid for the current state; refresh and review it before retrying.
- A 503 on verification means delivery failed; local mode requires Development and a writable `.local/outbox` folder. External mode requires the settings in the verification guide.
- SQL Server is a separate opt-in mode. See [database configuration](DATABASE.md); switching the provider does not transfer local data.
