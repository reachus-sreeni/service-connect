# Verification report

Prepared October 27, 2026 (America/Chicago).

## Local results

- Source recovered from an actual Downloads ZIP and a more complete local source folder; no reliance on broken sandbox download links.
- ASP.NET Core API Release build and publish succeeded using .NET SDK 10.0.403.
- Eleven backend tests passed: eight relational workflow cases and three HTTP integration cases (authentication, role/ownership isolation and the customer/provider estimate lifecycle).
- SQL Server concurrency test is explicitly skipped without TEST_SQLSERVER. Local machine has no Docker/SQL Server, so SQL Server runtime and locking behavior were not verified locally.
- Angular 22 production build succeeded.
- Three Angular UI tests passed using Vitest and a simulated DOM, including rendering customer/provider controls. These are component tests; a real browser end-to-end run against SQL Server was not performed.
- npm dependency audit reports zero known vulnerabilities after pinning Vite 8.3.5. This is a point-in-time scan, not a guarantee of security.
- Updated .NET packages restored without the vulnerability warnings seen in the original package versions.
- No AWS deployment was performed. AWS instructions are supplied; credentials/infrastructure are not part of this source package.

## GitHub verification

GitHub Actions run [40619136660](https://github.com/reachus-sreeni/service-connect/actions/runs/40619136660) passed for application commit `7980d9f871289fa2c8498df5d5721b6d4f9f51c2`:

- Backend: 11 API/relational workflow tests passed; the SQL-only case was skipped in this first pass.
- SQL Server 2022: all nine workflow/concurrency tests passed, including simultaneous acceptance with exactly one winner.
- ASP.NET Core Release publish succeeded.
- Angular production build and all three UI component tests passed.

The SQL Server checks ran on GitHub's Linux runner rather than the Mac. No real-browser end-to-end run against a deployed AWS environment was performed.

## Reproduction

```sh
dotnet test tests/ServiceConnect.Tests -c Release
dotnet publish backend/ServiceConnect.Api -c Release
cd frontend
npm ci
npm run build
npm test
```

To exercise SQL Server-specific workflow/locking checks, point TEST_SQLSERVER at a disposable supported SQL Server instance. Tests create uniquely named databases and delete those databases after running; the test login needs permission to create/drop them. Never use production credentials.

```sh
export TEST_SQLSERVER='Server=localhost,1433;User Id=sa;Password=YOUR_TEST_PASSWORD;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/ServiceConnect.Tests -c Release --filter FullyQualifiedName~WorkflowTests
```

CI's SQL Server credentials are disposable values for the isolated job container and are not application credentials. See GitHub Actions for the actual remote results; configuration alone does not mean a check passed.

## Publishing status

Published to [reachus-sreeni/service-connect, main](https://github.com/reachus-sreeni/service-connect) using GitHub CLI after device authorization. Existing repository history was preserved. The original connected integration rejected writes; GitHub CLI publishing succeeded.

The first SQL Server CI run exposed a nested EF Core deadlock exception that bypassed conflict handling. The workflow now translates wrapped SQL error 1205 into a 409 response after rolling back the failed transaction. The concurrency test requires exactly one successful acceptance and a handled conflict for the other attempt.

Current verification run: https://github.com/reachus-sreeni/service-connect/actions/runs/40619136660
