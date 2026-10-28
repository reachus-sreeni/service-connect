# Database modes and schema lifecycle

The same application and EF model support two providers. SQLite is a real relational database stored in a local file; it requires no database server. SQL Server is the server-backed option for a real environment. Separate EF migration sets are maintained because SQL types and migration operations differ by provider.

| Mode | Configuration | Initialization | Demo data |
| --- | --- | --- | --- |
| Local SQLite | Development Local launch profile, `Database:Provider=Sqlite` | Startup applies SQLite migrations | Seeded once into an empty account store |
| SQL Server | `Database:Provider=SqlServer`, SQL connection string | Explicit `--init-db` applies SQL Server migrations | Application demo seeding disabled |

## Switch to an empty SQL Server database

Stop the local API. Configure an actual supported SQL Server instance or private RDS SQL Server. SQL Server Linux container images are x64; ARM emulation on Apple Silicon is not officially supported. Local SQLite avoids that requirement.

In Terminal from the repository root:

```sh
export ASPNETCORE_ENVIRONMENT=Production
export Database__Provider=SqlServer
export Database__AutoInitialize=false
export Database__SeedDemo=false
export ConnectionStrings__SqlServer='Server=YOUR_HOST,1433;Database=ServiceConnect;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False'
export Jwt__Key="$(openssl rand -base64 48)"
export FrontendOrigin='http://localhost:4200'
export ASPNETCORE_URLS='http://localhost:5000'
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
dotnet run --no-launch-profile --project backend/ServiceConnect.Api -- --init-db
dotnet run --no-launch-profile --project backend/ServiceConnect.Api
```

`--no-launch-profile` is important: it prevents the Local launch profile from overriding the environment. Use a stable secret source for `Jwt__Key` outside a short test session. Do not commit credentials. Production must provide a signing key of at least 32 bytes; no default production key is generated. The connection must trust the SQL Server certificate through the appropriate certificate chain.

Initialization applies migrations and inserts the the configured service catalog (currently 34 categories and 227 options), but no demo accounts. It does not copy local users or transactions to SQL Server. The SQLite test file stays on disk separately. Use new registrations or an intentionally designed data import for a real environment.

Bootstrap a real administrator before running the application:

```sh
export BootstrapAdmin__Email='YOUR_ADMIN_EMAIL'
export BootstrapAdmin__Phone='YOUR_ADMIN_PHONE_IN_INTERNATIONAL_FORMAT'
export BootstrapAdmin__Password='YOUR_STRONG_ADMIN_PASSWORD'
dotnet run --no-launch-profile --project backend/ServiceConnect.Api -- --create-admin
unset BootstrapAdmin__Password
```

There is no public Admin registration option. Bootstrap does not mark contact details verified; the administrator can verify them through the normal verification workflow. Bootstrap refuses to overwrite an existing email/phone. Production provider approval is separate from contact verification.

Use a migration identity with schema permissions for `--init-db`/`--create-admin`; use a restricted runtime identity for normal application data access. Configure SMTP/Twilio before asking real users to verify their contacts. For AWS, use private networking, trusted TLS and secret references as described in the deployment guide.

## Existing databases from the earlier MVP

The previous repository used `EnsureCreated` without migration history and a smaller schema. Do not apply these initial migrations directly over that database: existing tables will conflict. Preserve/back up it and use a new empty database for this expanded version, or prepare a reviewed data-preserving baseline/import migration. The application will not drop or erase existing tables to force an upgrade.

Once initialized by this version, subsequent changes should add migrations for **both** providers. Never edit a migration already applied to shared/real data.

## Adding future migrations

Install the matching EF tool (`dotnet tool install --global dotnet-ef --version 10.0.14`) and update both sets after changing the model:

```sh
dotnet ef migrations add YOUR_CHANGE --context SqliteAppDb --project backend/ServiceConnect.Api --output-dir Migrations/Sqlite
dotnet ef migrations add YOUR_CHANGE --context SqlServerAppDb --project backend/ServiceConnect.Api --output-dir Migrations/SqlServer
```

Review generated SQL and test both providers before deployment. Design-time factories do not connect to a real database while generating migrations. At runtime `--init-db` uses the configured connection and correct migration context. Back up before changing a database containing real data.

Microsoft guidance: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers
