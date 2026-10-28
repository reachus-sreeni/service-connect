using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServiceConnect;
var builder = WebApplication.CreateBuilder(args);
var key = "";
builder.Services.AddDbContext<SqliteAppDb>(o => {
    var path = Path.Combine(builder.Environment.ContentRootPath, ".local");
    Directory.CreateDirectory(path);
    o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? $"Data Source={Path.Combine(path, "service-connect.db")}");
});
builder.Services.AddDbContext<SqlServerAppDb>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Set ConnectionStrings__SqlServer for SQL Server mode.")));
builder.Services.AddScoped<AppDb>(services => {
    var config = services.GetRequiredService<IConfiguration>();
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    var provider = config["Database:Provider"] ?? (environment.IsDevelopment() ? "Sqlite" : "SqlServer");
    return provider switch {
        "Sqlite" => services.GetRequiredService<SqliteAppDb>(),
        "SqlServer" => services.GetRequiredService<SqlServerAppDb>(),
        _ => throw new InvalidOperationException("Database:Provider must be Sqlite or SqlServer.")
    };
});
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICodeDelivery, CodeDelivery>();
builder.Services.AddScoped<VerificationService>();
builder.Services.AddScoped<Workflow>(); builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = "service-connect", ValidAudience = "service-connect", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(15) });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("AuthRateLimit:PermitLimit", 10), Window = TimeSpan.FromMinutes(1) })); });
var app = builder.Build();
key = app.Configuration["Jwt:Key"] ?? "";
if (string.IsNullOrEmpty(key) && app.Environment.IsDevelopment())
{
    var folder = Path.Combine(app.Environment.ContentRootPath, ".local"); Directory.CreateDirectory(folder);
    var path = Path.Combine(folder, "jwt.key");
    if (!File.Exists(path)) await File.WriteAllTextAsync(path, Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)));
    if (!OperatingSystem.IsWindows()) { File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
    key = await File.ReadAllTextAsync(path); app.Configuration["Jwt:Key"] = key;
}
if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Set Jwt__Key to a random secret of at least 32 bytes.");
var seedDemo = app.Configuration.GetValue<bool>("Database:SeedDemo");
if (seedDemo && !app.Environment.IsDevelopment()) throw new InvalidOperationException("Demo accounts can only be seeded in Development.");
var selectedProvider = app.Configuration["Database:Provider"] ?? (app.Environment.IsDevelopment() ? "Sqlite" : "SqlServer");
seedDemo = seedDemo && selectedProvider == "Sqlite";
if (args.Contains("--init-db") || args.Contains("--create-admin") || (app.Environment.IsDevelopment() && selectedProvider == "Sqlite" && app.Configuration.GetValue<bool>("Database:AutoInitialize")))
{
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.Database.MigrateAsync(); await DatabaseSetup.Catalog(db);
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
    if (seedDemo) await DatabaseSetup.Demo(db, hasher, app.Configuration["Demo:Password"] ?? "Local-Demo-Only-2026!");
    if (args.Contains("--create-admin")) await DatabaseSetup.CreateAdmin(db, app.Configuration, hasher);
    if (args.Contains("--init-db") || args.Contains("--create-admin")) return;
}
app.Use(async (ctx, next) => { try { await next(); } catch (RuleException ex) { ctx.Response.StatusCode = ex.Status; await ctx.Response.WriteAsJsonAsync(new { message = ex.Message }); } catch (DbUpdateException) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "Data changed or already exists. Refresh and retry." }); } catch (Exception ex) when (Workflow.IsDeadlock(ex) || ex is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 5 or 6 }) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "Concurrent change. Refresh and retry." }); } });
app.UseCors(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
Guid UserId(ClaimsPrincipal u) => Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
async Task RequireVerified(AppDb db, Guid id) {
    var account = await db.Accounts.FindAsync(id);
    if (account is null || !account.EmailVerified || !account.PhoneVerified) throw new RuleException(403, "Verify your email and phone first.");
}
object Session(Account a) { var token = new JwtSecurityToken("service-connect", "service-connect", new[] { new Claim(ClaimTypes.NameIdentifier, a.Id.ToString()), new Claim(ClaimTypes.Role, a.Role) }, expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)); return new { token = new JwtSecurityTokenHandler().WriteToken(token), a.Name, a.Role }; }
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/auth/register", async (RegisterInput i, AppDb db, IPasswordHasher<Account> h) =>
{
    if (string.IsNullOrWhiteSpace(i.Email) || i.Email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(i.Email, out var mail) || mail.Address != i.Email || string.IsNullOrWhiteSpace(i.Name) || i.Name.Length > 120 || i.Password is null || i.Password.Length < 12 || i.Password.Length > 128 || i.Role is not ("Customer" or "Provider") || string.IsNullOrEmpty(i.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(i.Phone, @"^\+[1-9]\d{7,14}$") || (i.Role == "Provider" && (string.IsNullOrWhiteSpace(i.BusinessName) || i.BusinessName.Length > 160 || string.IsNullOrWhiteSpace(i.Qualifications) || i.Qualifications.Length > 2000))) return Results.BadRequest(new { message = "Email, international phone (+country code), name (1–120), password (12–128) and role are required. Providers also need business name and qualifications." });
    var a = new Account { Email = i.Email.Trim().ToLowerInvariant(), Name = i.Name.Trim(), Phone = i.Phone, Role = i.Role }; a.PasswordHash = h.HashPassword(a, i.Password); db.Accounts.Add(a);
    if (a.Role == "Provider") db.Providers.Add(new ProviderProfile { AccountId = a.Id, BusinessName = i.BusinessName!.Trim(), Qualifications = i.Qualifications!.Trim() });
    await db.SaveChangesAsync(); return Results.Ok(Session(a));
}).RequireRateLimiting("auth");
app.MapPost("/api/auth/login", async (LoginInput i, AppDb db, IPasswordHasher<Account> h) =>
{
    var email = (i.Email ?? "").Trim().ToLowerInvariant(); var a = await db.Accounts.SingleOrDefaultAsync(x => x.Email == email);
    if (a == null || i.Password is null || h.VerifyHashedPassword(a, a.PasswordHash, i.Password) == PasswordVerificationResult.Failed) return Results.Json(new { message = "Invalid email or password." }, statusCode: 401);
    return Results.Ok(Session(a));
}).RequireRateLimiting("auth");
app.MapGet("/api/requests", async (AppDb db, ClaimsPrincipal u) => { var id = UserId(u); return await db.Requests.AsNoTracking().Where(r => u.IsInRole("Admin") || (u.IsInRole("Provider") && (r.Status == "Open" || db.Estimates.Any(e => e.Id == r.AcceptedEstimateId && e.ProviderId == id))) || r.CustomerId == id).OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(); }).RequireAuthorization();
app.MapPost("/api/requests", async (RequestInput i, AppDb db, ClaimsPrincipal u) =>
{
    if (string.IsNullOrWhiteSpace(i.Title) || i.Title.Length > 120 || string.IsNullOrWhiteSpace(i.Description) || i.Description.Length > 4000 || string.IsNullOrWhiteSpace(i.Location) || i.Location.Length > 200) return Results.BadRequest(new { message = "Title (1–120), description (1–4000), and location (1–200) are required." });
    await RequireVerified(db, UserId(u));
    if (!await db.Services.AnyAsync(x => x.Id == i.ServiceId)) return Results.BadRequest(new { message = "Choose a valid service category." });
    var r = new ServiceRequest { ServiceId = i.ServiceId, CustomerId = UserId(u), Title = i.Title.Trim(), Description = i.Description.Trim(), Location = i.Location.Trim() }; db.Requests.Add(r); db.Activities.Add(new ServiceActivity { RequestId = r.Id, ActorId = r.CustomerId, Kind = "RequestCreated", Details = "Customer posted a service request." }); await db.SaveChangesAsync(); return Results.Created($"/api/requests/{r.Id}", r);
}).RequireAuthorization(p => p.RequireRole("Customer"));
app.MapGet("/api/requests/{id:guid}/estimates", async (Guid id, AppDb db, ClaimsPrincipal u) =>
{
    var r = await db.Requests.FindAsync(id) ?? throw new RuleException(404, "Request not found."); var uid = UserId(u);
    if (u.IsInRole("Customer") && r.CustomerId != uid) throw new RuleException(403, "Request belongs to another customer.");
    return await db.Estimates.AsNoTracking().Where(e => e.RequestId == id && (u.IsInRole("Admin") || u.IsInRole("Customer") || e.ProviderId == uid)).Select(e => new { e.Id, e.RequestId, e.Revision, e.Amount, e.Notes, e.UpdatedAt, ProviderName = db.Accounts.Where(a => a.Id == e.ProviderId).Select(a => a.Name).First(), History = db.Revisions.Where(h => h.EstimateId == e.Id).OrderBy(h => h.Number).ToList() }).ToListAsync();
}).RequireAuthorization();
app.MapPut("/api/requests/{id:guid}/estimate", async (Guid id, EstimateInput i, Workflow w, ClaimsPrincipal u) => Results.Ok(await w.Quote(id, UserId(u), i))).RequireAuthorization(p => p.RequireRole("Provider"));
app.MapPost("/api/requests/{id:guid}/accept", async (Guid id, AcceptInput i, Workflow w, AppDb db, ClaimsPrincipal u) => { await RequireVerified(db, UserId(u)); await w.Accept(id, UserId(u), i); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Customer"));
ExtraEndpoints.Map(app);
app.Run();
public partial class Program { }
