# Run Service Connect on a Mac

## 1. Install prerequisites

Open Terminal. Check Git first:

```sh
git --version
```

If missing, run `xcode-select --install`, complete Apple's prompts, then check again. Install Node.js 22 LTS (22.22.3 or newer) from https://nodejs.org/en/download and the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0. Choose Arm64 for Apple Silicon, x64 for an Intel Mac. Reopen Terminal, then verify:

```sh
node --version
npm --version
dotnet --info
```

Clone the published repository:

```sh
git clone https://github.com/reachus-sreeni/service-connect.git
cd service-connect
```

## 2. Choose a SQL Server

**Apple Silicon:** SQL Server Linux container images are x64 and ARM emulation is not officially supported. Use a remote SQL Server on a supported x64 Linux/Windows machine or an AWS RDS SQL Server instance. For private RDS, connect through a VPN or an SSH/SSM tunnel from a VPC host; do not open port 1433 to the internet. The API runs natively on an Arm64 Mac. SQLite is only used by tests, not as a replacement for the application's SQL Server database.

**Intel Mac:** install and start Docker Desktop, then from the repository root:

```sh
cp .env.example .env
```

Edit `.env` and replace its placeholder with a strong SQL Server password. Then:

```sh
docker compose up -d sqlserver
docker compose logs -f sqlserver
```

Wait for “SQL Server is now ready for client connections,” then press Ctrl+C to stop viewing logs (the database keeps running). Developer edition is for development only. Database files persist in the `sql-data` volume.

## 3. Configure and initialize the API

In the same Terminal, set the connection string for your database. This example is for the local development container. Replace the password. For a remote server use its hostname, a suitable database user, `Encrypt=True` and `TrustServerCertificate=False` with a valid server certificate.

```sh
export ConnectionStrings__SqlServer='Server=localhost,1433;Database=ServiceConnect;User Id=sa;Password=YOUR_LOCAL_PASSWORD;Encrypt=True;TrustServerCertificate=True'
export Jwt__Key="$(openssl rand -base64 48)"
export FrontendOrigin='http://localhost:4200'
export ASPNETCORE_URLS='http://localhost:5000'
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
dotnet run --project backend/ServiceConnect.Api -- --init-db
dotnet run --project backend/ServiceConnect.Api
```

The initialization command creates a new database schema; the account needs database creation/schema permissions. For production use a separate initialization identity and restrict the runtime identity to application tables. The API fails at startup without a JWT key of at least 32 bytes. Keep environment settings in your Terminal or an ignored private configuration file. Do not commit secrets.

Check http://localhost:5000/health (process health; it does not check the database).

## 4. Start Angular in a second Terminal

From the project root:

```sh
cd frontend
npm ci
npm start
```

Open http://localhost:4200. The development proxy forwards `/api` to localhost:5000. API and frontend must both be running. Use localhost rather than a LAN address.

## 5. Exercise the workflow

1. Register a Customer with a password of at least 12 characters. Post a request.
2. In a separate browser/private window register a Provider. Open the request and submit an estimate.
3. Revise the estimate; the same estimate keeps its history.
4. In the customer window refresh, compare and accept the current revision.
5. Refresh the provider window: further edits are closed. A second acceptance is rejected by the server.

Create another provider to compare multiple estimates. Providers see only their own estimates; a customer sees all estimates for their own requests. No payment information is collected.

## Troubleshooting

- Database connection fails: verify the hostname, port, credentials, network tunnel and SQL Server readiness.
- Login/register fails with 429: wait a minute; authentication is limited to 10 requests per minute per client IP per API instance.
- 409 after a revision/acceptance: refresh before retrying; another action changed the request.
- Browser reports 502/proxy error: check that the API is running on port 5000.
- Stop API/UI with Ctrl+C; stop local SQL with `docker compose stop`. Do not delete the database volume unless you intend to erase all data.

If npm 10 fails while resolving optional peers, update npm with `npm install -g npm@11`, then retry `npm ci`. Angular's disk build cache is disabled in this project because its native cache library crashed on the development Mac; this does not affect the deployed frontend.
