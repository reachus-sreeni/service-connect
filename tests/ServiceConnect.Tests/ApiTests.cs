using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class ApiFactory : WebApplicationFactory<Program> {
 static ApiFactory() => Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
 readonly SqliteConnection connection = new("Data Source=:memory:");
 protected override void ConfigureWebHost(IWebHostBuilder builder) {
  connection.Open();
  builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> { ["Jwt:Key"] = "integration-tests-only-secret-key-at-least-32-bytes" }));
  builder.ConfigureServices(services => {
   services.RemoveAll<DbContextOptions<AppDb>>();
   services.RemoveAll<IDbContextOptionsConfiguration<AppDb>>();
   services.RemoveAll<AppDb>();
   services.AddDbContext<AppDb>(o => o.UseSqlite(connection));
  });
 }
 public HttpClient Ready() { var client = CreateClient(); using var scope = Services.CreateScope(); scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated(); return client; }
 protected override void Dispose(bool disposing) { base.Dispose(disposing); if(disposing) connection.Dispose(); }
}
public class ApiTests {
 static async Task<string> Register(HttpClient c, string role) {
  var r = await c.PostAsJsonAsync("/api/auth/register", new {email=Guid.NewGuid()+"@example.com", name=role, password="Strong-Test-Password-123", role});
  r.EnsureSuccessStatusCode(); return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
 }
 static void SignIn(HttpClient c,string token) => c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",token);
 [Fact] public async Task AnonymousAndWrongRoleCannotCreateRequests() {
  using var host=new ApiFactory();using var c=host.Ready();
  Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/requests")).StatusCode);
  SignIn(c,await Register(c,"Provider"));
  Assert.Equal(HttpStatusCode.Forbidden,(await c.PostAsJsonAsync("/api/requests",new {title="Repair",description="Door",location="Austin"})).StatusCode);
 }
 [Fact] public async Task CompleteCustomerProviderWorkflowAndIsolation() {
  using var host=new ApiFactory();using var c=host.Ready();
  var owner=await Register(c,"Customer");SignIn(c,owner);
  var requestResponse=await c.PostAsJsonAsync("/api/requests",new {title="Repair door",description="Replace hinge",location="Austin"});requestResponse.EnsureSuccessStatusCode();
  var id=(await requestResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
  var other=await Register(c,"Customer");SignIn(c,other);
  Assert.Empty(await c.GetFromJsonAsync<JsonElement[]>("/api/requests") ?? []);
  Assert.Equal(HttpStatusCode.Forbidden,(await c.GetAsync($"/api/requests/{id}/estimates")).StatusCode);
  var provider=await Register(c,"Provider");SignIn(c,provider);
  var quote=await c.PutAsJsonAsync($"/api/requests/{id}/estimate",new {amount=100,notes="Parts and labor",expectedRevision=0});quote.EnsureSuccessStatusCode();
  var estimateId=(await quote.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
  (await c.PutAsJsonAsync($"/api/requests/{id}/estimate",new {amount=125,notes="Includes new hinge",expectedRevision=1})).EnsureSuccessStatusCode();
  // A second provider cannot see the first provider's quote.
  SignIn(c,await Register(c,"Provider"));
  Assert.Empty(await c.GetFromJsonAsync<JsonElement[]>($"/api/requests/{id}/estimates") ?? []);
  SignIn(c,owner);
  var estimates=await c.GetFromJsonAsync<JsonElement[]>($"/api/requests/{id}/estimates");Assert.Single(estimates!);Assert.Equal(2,estimates![0].GetProperty("history").GetArrayLength());
  Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/requests/{id}/accept",new {estimateId,expectedRevision=1})).StatusCode);
  Assert.Equal(HttpStatusCode.NoContent,(await c.PostAsJsonAsync($"/api/requests/{id}/accept",new {estimateId,expectedRevision=2})).StatusCode);
  SignIn(c,provider);
  Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/requests/{id}/estimate",new {amount=140,notes="Too late",expectedRevision=2})).StatusCode);
 }
 [Fact] public async Task InvalidRegistrationAndWrongPasswordAreRejected() {
  using var host=new ApiFactory();using var c=host.Ready();
  Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/auth/register",new {email="bad",name="Name",password="short",role="Admin"})).StatusCode);
  var r=await c.PostAsJsonAsync("/api/auth/register",new {email="person@example.com",name="Person",password="Strong-Password-123",role="Customer"});r.EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Unauthorized,(await c.PostAsJsonAsync("/api/auth/login",new {email="person@example.com",password="wrong"})).StatusCode);
  (await c.PostAsJsonAsync("/api/auth/login",new {email="person@example.com",password="Strong-Password-123"})).EnsureSuccessStatusCode();
 }
}
