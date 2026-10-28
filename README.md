# Service Connect

A service marketplace portfolio application using Angular 22, ASP.NET Core 10 C#, and EF Core. Customers register, verify phone and email, post requests, compare estimates and accept one. Verified, administrator-approved providers quote and revise estimates, perform the work and mark it done. The customer confirms completion with mandatory ratings and written feedback. The accepted provider can then rate the customer on communication, cooperation and appointment readiness.

There are no payments. Service “transactions” are the recorded activity history for requests, quotes, revisions, acceptance, work completion and feedback.

## Run locally without SQL Server

Install Node.js 22 LTS (22.22.3+) and the .NET 10 SDK, clone this repository, then:

```sh
dotnet run --project backend/ServiceConnect.Api
```

In another Terminal:

```sh
cd frontend
npm ci
npm start
```

Open http://localhost:4200. The default local launch profile uses a SQLite **file** at `backend/ServiceConnect.Api/.local/service-connect.db`. It applies SQLite migrations and seeds synthetic data once. Your changes persist across restarts. No SQL Server or Docker is required for local testing. Local data, verification codes and the generated signing key are excluded from Git and container builds.

## Demo accounts

All use password **`Local-Demo-Only-2026!`** unless `Demo__Password` was set before the first seed.

| Email | Role | Starting state |
| --- | --- | --- |
| admin@example.test | Administrator | Can review providers |
| alex@example.test | Customer | Verified; owns sample requests |
| jamie@example.test | Customer | Verified; separate account for access checks |
| morgan@example.test | Provider | Verified and approved |
| taylor@example.test | Provider | Verified and approved; competing quote |
| casey@example.test | Provider | Unverified and pending approval |

Examples cover Open, Accepted, AwaitingCustomerReview and Completed requests, estimate revisions, activity history and two-way reviews. These accounts are created only in Development with SQLite. Restarts do not reset your tests or overwrite account data.

## UI/UX and portfolio walkthrough

Browse before signing in, search within service options, then use the guided request flow. The featured catalog contains Plumbing, Gardening, Cleaning, Carpentry and Automotive Service, each with five selectable services. Electrical remains under Other services for existing records. Catalog changes keep stable IDs and preserve older requests without an option.

- [Design system and component contracts](docs/DESIGN_SYSTEM.md)
- [Accessibility implementation and audit plan](docs/ACCESSIBILITY.md)
- [Job-aligned interview walkthrough](docs/INTERVIEW_WALKTHROUGH.md)

## Guides

- [Local setup and manual test flow](docs/LOCAL_SETUP.md)
- [Schema and relationships](docs/SCHEMA.md)
- [Contact verification and administrator approval](docs/VERIFICATION_SETUP.md)
- [Switching to SQL Server](docs/DATABASE.md)
- [Git installation and publishing](docs/GITHUB.md)
- [AWS deployment](docs/AWS_DEPLOYMENT.md)
- [Architecture and API](docs/ARCHITECTURE.md)
- [Build and verification report](docs/VERIFICATION.md)

GitHub `main` is the source of truth. Work continues in this repository; ZIP archives are not regenerated for ongoing changes.

## Tests

```sh
dotnet test tests/ServiceConnect.Tests -c Release
dotnet publish backend/ServiceConnect.Api -c Release
cd frontend
npm ci
npm test
npm run build
```

Tests cover contact verification, provider approval, service completion, required feedback, authorization, file persistence and migrations. GitHub Actions also tests SQL Server workflow behavior and simultaneous acceptance. UI tests use Vitest and a simulated DOM.

Sessions last one hour and stay in memory; refreshing the browser signs out. The application does not yet include password recovery, moderation, payment processing or a production identity/background-check service. Administrator approval records a review decision; it does not certify legal credentials. Service lists are limited to the latest 200 requests. Real SMTP/SMS delivery requires configured providers; local codes are written to a private test outbox.
