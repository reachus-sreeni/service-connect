# Service Connect

A portfolio application connecting customers with local service providers. Angular 22 frontend, ASP.NET Core 10 C# API, EF Core 10 and SQL Server.

Customers register, create requests, compare estimates and accept exactly one. Providers register, browse requests and maintain one estimate per request, with an immutable revision history. Acceptance checks the revision the customer reviewed and freezes further estimates. There are no payments.

## Start here

- [Mac setup](docs/LOCAL_SETUP.md) — prerequisites, database, API and Angular
- [Git installation and publishing](docs/GITHUB.md)
- [AWS deployment](docs/AWS_DEPLOYMENT.md) — container, private RDS SQL Server, HTTPS frontend
- [Architecture and API](docs/ARCHITECTURE.md)
- [Verification report](docs/VERIFICATION.md)

```text
frontend/                       Angular UI and UI tests
backend/ServiceConnect.Api/      API, EF models, transaction workflow, Dockerfile
tests/ServiceConnect.Tests/      Workflow and HTTP integration tests
compose.yaml                    Local SQL Server (x64 only officially supported)
.github/workflows/ci.yml         Build and tests
```

## Verify

```sh
dotnet test tests/ServiceConnect.Tests -c Release
dotnet publish backend/ServiceConnect.Api -c Release
cd frontend
npm ci
npm run build
npm test
```

Frontend tests use Vitest with a simulated DOM and do not require a separate browser. API tests use an isolated SQLite relational database; they do not prove SQL Server locking behavior. CI is configured to run workflow tests against SQL Server after publication. See the verification report for what actually ran.

## Portfolio scope

JWT sessions last one hour and stay in memory; refreshing the browser signs out. Passwords use ASP.NET Core Identity's password hasher. Endpoints enforce roles and customer ownership. Unique database indexes enforce one estimate per provider/request. Serializable transactions plus a request concurrency token prevent conflicting workflow changes; clients refresh after a 409. Requests show at most the newest 200 entries; pagination, email verification, password recovery, refresh tokens, moderation and notifications are future work. Avoid putting street addresses or other sensitive information in request descriptions because registered providers can read them.

Database setup is an explicit one-time `--init-db` operation using EF `EnsureCreated`. This is suitable for a new portfolio database, not an upgrade strategy. Introduce reviewed EF migrations before evolving a database containing real data. Never delete a populated database to apply a schema change.
