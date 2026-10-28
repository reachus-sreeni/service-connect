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
builder.Services.AddDbContext<AppDb>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddScoped<Workflow>(); builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = "service-connect", ValidAudience = "service-connect", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(15) });
builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("auth", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) })); });
var app = builder.Build();
key = app.Configuration["Jwt:Key"] ?? "";
if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Set Jwt__Key to a random secret of at least 32 bytes.");
if (args.Contains("--init-db")) { using var scope = app.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreatedAsync(); return; }
app.Use(async (ctx, next) => { try { await next(); } catch (RuleException ex) { ctx.Response.StatusCode = ex.Status; await ctx.Response.WriteAsJsonAsync(new { message = ex.Message }); } catch (DbUpdateException) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "Data changed or already exists. Refresh and retry." }); } catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205) { ctx.Response.StatusCode = 409; await ctx.Response.WriteAsJsonAsync(new { message = "Concurrent change. Refresh and retry." }); } });
app.UseCors(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
Guid UserId(ClaimsPrincipal u) => Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
object Session(Account a) { var token = new JwtSecurityToken("service-connect", "service-connect", new[] { new Claim(ClaimTypes.NameIdentifier, a.Id.ToString()), new Claim(ClaimTypes.Role, a.Role) }, expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)); return new { token = new JwtSecurityTokenHandler().WriteToken(token), a.Name, a.Role }; }
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/auth/register", async (RegisterInput i, AppDb db, IPasswordHasher<Account> h) =>
{
    if (string.IsNullOrWhiteSpace(i.Email) || i.Email.Length > 254 || !System.Net.Mail.MailAddress.TryCreate(i.Email, out var mail) || mail.Address != i.Email || string.IsNullOrWhiteSpace(i.Name) || i.Name.Length > 120 || i.Password is null || i.Password.Length < 12 || i.Password.Length > 128 || i.Role is not ("Customer" or "Provider")) return Results.BadRequest(new { message = "Use a valid email, name (1–120), password (12–128), and Customer or Provider role." });
    var a = new Account { Email = i.Email.Trim().ToLowerInvariant(), Name = i.Name.Trim(), Role = i.Role }; a.PasswordHash = h.HashPassword(a, i.Password); db.Accounts.Add(a); await db.SaveChangesAsync(); return Results.Ok(Session(a));
}).RequireRateLimiting("auth");
app.MapPost("/api/auth/login", async (LoginInput i, AppDb db, IPasswordHasher<Account> h) =>
{
    var email = (i.Email ?? "").Trim().ToLowerInvariant(); var a = await db.Accounts.SingleOrDefaultAsync(x => x.Email == email);
    if (a == null || i.Password is null || h.VerifyHashedPassword(a, a.PasswordHash, i.Password) == PasswordVerificationResult.Failed) return Results.Json(new { message = "Invalid email or password." }, statusCode: 401);
    return Results.Ok(Session(a));
}).RequireRateLimiting("auth");
app.MapGet("/api/requests", async (AppDb db, ClaimsPrincipal u) => { var id = UserId(u); return await db.Requests.AsNoTracking().Where(r => u.IsInRole("Provider") || r.CustomerId == id).OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(); }).RequireAuthorization();
app.MapPost("/api/requests", async (RequestInput i, AppDb db, ClaimsPrincipal u) =>
{
    if (string.IsNullOrWhiteSpace(i.Title) || i.Title.Length > 120 || string.IsNullOrWhiteSpace(i.Description) || i.Description.Length > 4000 || string.IsNullOrWhiteSpace(i.Location) || i.Location.Length > 200) return Results.BadRequest(new { message = "Title (1–120), description (1–4000), and location (1–200) are required." });
    var r = new ServiceRequest { CustomerId = UserId(u), Title = i.Title.Trim(), Description = i.Description.Trim(), Location = i.Location.Trim() }; db.Requests.Add(r); await db.SaveChangesAsync(); return Results.Created($"/api/requests/{r.Id}", r);
}).RequireAuthorization(p => p.RequireRole("Customer"));
app.MapGet("/api/requests/{id:guid}/estimates", async (Guid id, AppDb db, ClaimsPrincipal u) =>
{
    var r = await db.Requests.FindAsync(id) ?? throw new RuleException(404, "Request not found."); var uid = UserId(u);
    if (u.IsInRole("Customer") && r.CustomerId != uid) throw new RuleException(403, "Request belongs to another customer.");
    return await db.Estimates.AsNoTracking().Where(e => e.RequestId == id && (u.IsInRole("Customer") || e.ProviderId == uid)).Select(e => new { e.Id, e.RequestId, e.Revision, e.Amount, e.Notes, e.UpdatedAt, ProviderName = db.Accounts.Where(a => a.Id == e.ProviderId).Select(a => a.Name).First(), History = db.Revisions.Where(h => h.EstimateId == e.Id).OrderBy(h => h.Number).ToList() }).ToListAsync();
}).RequireAuthorization();
app.MapPut("/api/requests/{id:guid}/estimate", async (Guid id, EstimateInput i, Workflow w, ClaimsPrincipal u) => Results.Ok(await w.Quote(id, UserId(u), i))).RequireAuthorization(p => p.RequireRole("Provider"));
app.MapPost("/api/requests/{id:guid}/accept", async (Guid id, AcceptInput i, Workflow w, ClaimsPrincipal u) => { await w.Accept(id, UserId(u), i); return Results.NoContent(); }).RequireAuthorization(p => p.RequireRole("Customer"));
app.Run();
public partial class Program { }
