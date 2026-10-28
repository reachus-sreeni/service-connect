using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public sealed class TestDelivery : ICodeDelivery {
 public ConcurrentDictionary<Guid,string> Codes { get; } = new();
 public Task Send(string channel,string destination,string code,Guid id) { Codes[id]=code;return Task.CompletedTask; }
}
public class ApiFactory : WebApplicationFactory<Program> {
 static ApiFactory() => Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
 readonly string? sql = Environment.GetEnvironmentVariable("TEST_SQLSERVER") is { } value ? new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(value){InitialCatalog="ServiceConnectApiTest_"+Guid.NewGuid().ToString("N")}.ConnectionString : null;
 readonly SqliteConnection connection = new("Data Source=:memory:");
 public TestDelivery Delivery { get; } = new();
 protected override void ConfigureWebHost(IWebHostBuilder builder) {
  connection.Open();builder.UseEnvironment("Testing");
  builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> {
   ["Jwt:Key"]="integration-tests-only-secret-key-at-least-32-bytes",["Database:SeedDemo"]="false",["Database:AutoInitialize"]="false",["AuthRateLimit:PermitLimit"]="1000"
  }));
  builder.ConfigureServices(services => {
   services.RemoveAll<AppDb>();services.AddScoped<AppDb>(_ => sql is null
    ? new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlite(connection).Options)
    : new SqlServerAppDb(new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer(sql).Options));
   services.RemoveAll<ICodeDelivery>();services.AddSingleton<ICodeDelivery>(Delivery);
  });
 }
 public HttpClient Ready() {
  var client=CreateClient();using var scope=Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDb>();
  if(sql is null)db.Database.EnsureCreated();else db.Database.Migrate();DatabaseSetup.Catalog(db).GetAwaiter().GetResult();
  var admin=new Account{Email="admin@example.test",Phone="+12025550100",Role="Admin",Name="Administrator"};
  admin.PasswordHash=new PasswordHasher<Account>().HashPassword(admin,"Strong-Test-Password-123");db.Accounts.Add(admin);db.SaveChanges();return client;
 }
 protected override void Dispose(bool disposing) { base.Dispose(disposing);if(disposing){connection.Dispose();if(sql is not null){using var db=new SqlServerAppDb(new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer(sql).Options);db.Database.EnsureDeleted();}} }
}
public class ApiTests {
 const string Password="Strong-Test-Password-123";
 static int sequence=100;
 static void SignIn(HttpClient c,string token) => c.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);
 static async Task<string> Register(HttpClient c,string role) {
  var number=Interlocked.Increment(ref sequence);
  var response=await c.PostAsJsonAsync("/api/auth/register",new {email=$"user{number}@example.test",phone="+1202555"+number.ToString("D4"),name=role,password=Password,role,businessName="Test business",qualifications="Test experience"});
  response.EnsureSuccessStatusCode();var token=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;SignIn(c,token);return token;
 }
 static async Task VerifyContacts(ApiFactory host,HttpClient c) {
  foreach(var channel in new[]{"email","phone"}) {
   var response=await c.PostAsync($"/api/me/verification/{channel}/send",null);response.EnsureSuccessStatusCode();
   var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("challengeId").GetGuid();
   (await c.PostAsJsonAsync($"/api/me/verification/{channel}/confirm",new{challengeId=id,code=host.Delivery.Codes[id]})).EnsureSuccessStatusCode();
  }
 }
 static async Task<string> Admin(HttpClient c) {
  var r=await c.PostAsJsonAsync("/api/auth/login",new{email="admin@example.test",password=Password});r.EnsureSuccessStatusCode();
  var token=(await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;SignIn(c,token);return token;
 }
 static Task<HttpResponseMessage> CreateRequest(HttpClient c) => c.PostAsJsonAsync("/api/requests",new{title="Repair door",description="Replace hinge",location="Demo area",serviceId=DatabaseSetup.GeneralServiceId,serviceOptionId=DatabaseSetup.HingeRepairOptionId});
 [Fact] public async Task PublicCatalogHasRequestedCategoriesAndScopedOptions() {
  using var host=new ApiFactory();using var c=host.Ready();
  var catalog=await c.GetFromJsonAsync<JsonElement[]>("/api/services");
  Assert.Equal(5,catalog!.Count(c=>c.GetProperty("isFeatured").GetBoolean()));
  foreach(var name in new[]{"Plumbing","Gardening","Cleaning","Carpentry","Automotive Service"}) Assert.Contains(catalog!,c=>c.GetProperty("name").GetString()==name&&c.GetProperty("options").GetArrayLength()==5);
  await Register(c,"Customer");await VerifyContacts(host,c);
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/requests",new{title="Repair",description="Leak",location="Demo",serviceId=DatabaseSetup.GeneralServiceId,serviceOptionId=Guid.Parse("40000000-0000-0000-0000-000000000101")})).StatusCode);
 }
 [Fact] public async Task AnonymousAndWrongRoleCannotCreateRequests() {
  using var host=new ApiFactory();using var c=host.Ready();
  Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/requests")).StatusCode);
  await Register(c,"Provider");Assert.Equal(HttpStatusCode.Forbidden,(await CreateRequest(c)).StatusCode);
 }
 [Fact] public async Task CompleteVerifiedAndApprovedLifecycleWithRequiredTwoWayFeedback() {
  using var host=new ApiFactory();using var c=host.Ready();
  var owner=await Register(c,"Customer");Assert.Equal(HttpStatusCode.Forbidden,(await CreateRequest(c)).StatusCode);
  await VerifyContacts(host,c);var response=await CreateRequest(c);response.EnsureSuccessStatusCode();
  var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
  var other=await Register(c,"Customer");Assert.Empty(await c.GetFromJsonAsync<JsonElement[]>("/api/requests")??[]);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/requests/{id}/history")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/requests/{id}/estimates")).StatusCode);
  var provider=await Register(c,"Provider");var me=await c.GetFromJsonAsync<JsonElement>("/api/me");var providerId=me.GetProperty("id").GetGuid();
  var quotePath=$"/api/requests/{id}/estimate";
  Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync(quotePath,new{amount=100,notes="Scope",expectedRevision=0})).StatusCode);
  var admin=await Admin(c);
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/admin/providers/{providerId}/decision",new{approve=true,notes="Reviewed"})).StatusCode);
  SignIn(c,provider);await VerifyContacts(host,c);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.PutAsJsonAsync(quotePath,new{amount=100,notes="Scope",expectedRevision=0})).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync("/api/admin/providers")).StatusCode);
  SignIn(c,admin);(await c.PostAsJsonAsync($"/api/admin/providers/{providerId}/decision",new{approve=true,notes="Qualifications reviewed"})).EnsureSuccessStatusCode();
  SignIn(c,provider);response=await c.PutAsJsonAsync(quotePath,new{amount=100,notes="Parts and labor",expectedRevision=0});response.EnsureSuccessStatusCode();
  var estimateId=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
  (await c.PutAsJsonAsync(quotePath,new{amount=125,notes="Includes hinge",expectedRevision=1})).EnsureSuccessStatusCode();
  await Register(c,"Provider");Assert.Empty(await c.GetFromJsonAsync<JsonElement[]>($"/api/requests/{id}/estimates")??[]);
  SignIn(c,owner);
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/requests/{id}/accept",new{estimateId,expectedRevision=1})).StatusCode);
  (await c.PostAsJsonAsync($"/api/requests/{id}/accept",new{estimateId,expectedRevision=2})).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/requests/{id}/complete",new{overall=5,quality=5,communication=5,punctuality=5,feedback="Great"})).StatusCode);
  SignIn(c,provider);Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync(quotePath,new{amount=140,notes="Late",expectedRevision=2})).StatusCode);
  (await c.PostAsync($"/api/requests/{id}/work-done",null)).EnsureSuccessStatusCode();
  SignIn(c,other);Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync($"/api/requests/{id}/complete",new{overall=5,quality=5,communication=5,punctuality=5,feedback="Great"})).StatusCode);
  SignIn(c,owner);Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync($"/api/requests/{id}/complete",new{overall=0,quality=5,communication=5,punctuality=5,feedback=""})).StatusCode);
  (await c.PostAsJsonAsync($"/api/requests/{id}/complete",new{overall=5,quality=4,communication=5,punctuality=4,feedback="Careful work"})).EnsureSuccessStatusCode();
  SignIn(c,provider);var review=new{overall=5,communication=4,cooperation=5,readiness=4,feedback="Clear requirements"};
  (await c.PostAsJsonAsync($"/api/requests/{id}/customer-review",review)).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/requests/{id}/customer-review",review)).StatusCode);
  var history=await c.GetFromJsonAsync<JsonElement>($"/api/requests/{id}/history");Assert.Equal(2,history.GetProperty("reviews").GetArrayLength());Assert.Equal(7,history.GetProperty("activities").GetArrayLength());Assert.EndsWith("Z",history.GetProperty("activities")[0].GetProperty("createdAt").GetString());
  Assert.Equal("Completed",(await c.GetFromJsonAsync<JsonElement[]>("/api/requests"))!.Single().GetProperty("status").GetString());
 }
 [Fact] public async Task CodesAreSingleUseBoundToAccountExpireAndLimitAttempts() {
  using var host=new ApiFactory();using var c=host.Ready();
  var owner=await Register(c,"Customer");var response=await c.PostAsync("/api/me/verification/email/send",null);response.EnsureSuccessStatusCode();
  var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("challengeId").GetGuid();var code=host.Delivery.Codes[id];
  await Register(c,"Customer");Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/me/verification/email/confirm",new{challengeId=id,code})).StatusCode);
  SignIn(c,owner);(await c.PostAsJsonAsync("/api/me/verification/email/confirm",new{challengeId=id,code})).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/me/verification/email/confirm",new{challengeId=id,code})).StatusCode);
  response=await c.PostAsync("/api/me/verification/phone/send",null);id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("challengeId").GetGuid();code=host.Delivery.Codes[id];
  var wrong=code=="000000"?"111111":"000000";
  for(var i=0;i<5;i++)Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/me/verification/phone/confirm",new{challengeId=id,code=wrong})).StatusCode);
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/me/verification/phone/confirm",new{challengeId=id,code})).StatusCode);
  Assert.Equal(HttpStatusCode.TooManyRequests,(await c.PostAsync("/api/me/verification/phone/send",null)).StatusCode);
  var account=await c.GetFromJsonAsync<JsonElement>("/api/me");Assert.True(account.GetProperty("emailVerified").GetBoolean());Assert.False(account.GetProperty("phoneVerified").GetBoolean());
 }
 [Fact] public async Task ExpiredCodeCannotVerifyContact() {
  using var host=new ApiFactory();using var c=host.Ready();await Register(c,"Customer");
  var response=await c.PostAsync("/api/me/verification/email/send",null);response.EnsureSuccessStatusCode();
  var id=(await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("challengeId").GetGuid();
  using(var scope=host.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AppDb>();var challenge=await db.VerificationChallenges.FindAsync(id);challenge!.ExpiresAt=DateTime.UtcNow.AddMinutes(-1);await db.SaveChangesAsync(); }
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/me/verification/email/confirm",new{challengeId=id,code=host.Delivery.Codes[id]})).StatusCode);
  Assert.False((await c.GetFromJsonAsync<JsonElement>("/api/me")).GetProperty("emailVerified").GetBoolean());
 }
 [Fact] public async Task RegistrationRequiresPhoneEmailAndCannotGrantAdmin() {
  using var host=new ApiFactory();using var c=host.Ready();
  foreach(var input in new[]{new{email="a@example.test",phone="",name="Name",password=Password,role="Customer"},new{email="bad",phone="+12025550109",name="Name",password=Password,role="Customer"},new{email="a@example.test",phone="+12025550109",name="Name",password=Password,role="Admin"}})
   Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/auth/register",input)).StatusCode);
  var registration=new{email="person@example.test",phone="+12025550109",name="Person",password=Password,role="Customer"};
  (await c.PostAsJsonAsync("/api/auth/register",registration)).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/auth/register",new{email="other@example.test",phone=registration.phone,name="Other",password=Password,role="Customer"})).StatusCode);
  Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/auth/login",new{email=registration.email,password="wrong"})).StatusCode);
  (await c.PostAsJsonAsync("/api/auth/login",new{email=registration.email,password=Password})).EnsureSuccessStatusCode();
 }
}
