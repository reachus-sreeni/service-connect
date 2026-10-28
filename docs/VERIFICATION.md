# Verification report

Updated October 27, 2026 (America/Chicago). GitHub main is the source of truth; no new ZIP is generated for this feature update.

## Local checks for the expanded application

- ASP.NET Core build and Release publish succeeded.
- Eighteen backend tests passed, covering required phone/email, contact verification, code replay/expiry/attempt limits, administrator approval gates, customer/provider isolation, estimates, completion and two-way reviews.
- The local file test applies SQLite migrations, seeds all lifecycle states, edits a request, closes/reopens the database and verifies persistence without duplicate seeding.
- Eight Angular component tests passed, including required provider registration fields, verification/approval gates and mandatory completion feedback.
- Angular production build passed.
- The actual Development API started with SQLite migrations and synthetic demo data, without SQL Server or Docker.
- A real browser signed in as the demo customer and displayed persisted requests, completed feedback in both directions and service history.
- File outbox tests verify private local code files and reject LocalOutbox in Production.
- A live browser sign-in check found and corrected the frontend's inherited proxy target (5080) to the API's actual local port (5000).

The SQL-only concurrency test is skipped on the Mac, which has no local SQL Server. GitHub Actions runs the full backend suite a second time against SQL Server, including provider-specific migrations and simultaneous acceptance. See [the current workflow runs](https://github.com/reachus-sreeni/service-connect/actions) for remote results.

## Delivery and deployment limits

Local verification uses the private file outbox; automated tests capture codes without sending messages. Actual SMTP and Twilio delivery require configured credentials and were not tested against live services. Administrator approval reviews supplied qualifications; it is not an identity/background-check service.

No AWS resources were deployed. The AWS guide describes SQL Server, secrets, real verification delivery and frontend/API routing. Switching from SQLite to SQL Server selects a separate database; it does not transfer local test data automatically.

## Reproduction

```sh
dotnet test tests/ServiceConnect.Tests -c Release
dotnet publish backend/ServiceConnect.Api -c Release
cd frontend
npm ci
npm test
npm run build
```

For SQL Server tests, set TEST_SQLSERVER to a disposable instance. The tests create unique databases, apply SQL Server migrations and delete those test databases afterward. Use a test login with create/drop permissions, never production credentials:

```sh
export TEST_SQLSERVER='Server=localhost,1433;User Id=sa;Password=YOUR_TEST_PASSWORD;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/ServiceConnect.Tests -c Release
```

CI's SQL credentials are disposable values for its isolated container, not application credentials. See LOCAL_SETUP.md for the manual flow through test accounts, OTP outbox, approval, work completion and both reviews.

## UI/UX and catalog update

The redesign adds five featured categories with five options each, an additional legacy Electrical category, a public catalog, a guided request wizard, separate account/workspace views and shared Sass/UI primitives. Backend checks include catalog option scope and a migration from the previous schema that preserves a pre-existing customer and request.

The new full-stack Playwright suite contains seven scenarios with axe checks tagged for WCAG 2.1 A/AA. Local execution starts the isolated application correctly but macOS denies Chromium launch, so local automated-browser success is not claimed. The GitHub frontend job installs Chromium on Linux, runs the suite and uploads its report. JAWS, complete manual conformance testing and Azure DevOps execution remain unverified; see ACCESSIBILITY.md and INTERVIEW_WALKTHROUGH.md.

The initial Linux axe run caught a visible-label/accessibility-name mismatch on the brand control. The brand now uses its visible content for its accessible name, and service-option action names include the visible “Request estimates” label plus the option name. The wizard error-summary heading also follows the page heading hierarchy.
