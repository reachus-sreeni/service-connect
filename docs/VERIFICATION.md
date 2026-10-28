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

GitHub repository read access was verified, but both Git data and file-write APIs returned HTTP 403, “Resource not accessible by integration.” The repository reported user-level push access, which does not guarantee the connected app has content-write permission. A local Git push also failed because no HTTPS username/credentials were available. The local checkout preserves the remote initial commit and has the application committed on main. No GitHub upload is claimed. GitHub Actions and SQL Server CI results remain unverified until a push succeeds.
