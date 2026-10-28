using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class CatalogConfigurationTests {
 [Fact] public async Task EveryReviewedIndiaOfferingResolvesAndRepeatedSyncPreservesRequests() {
  var sql=Environment.GetEnvironmentVariable("TEST_SQLSERVER");
  using var connection=new SqliteConnection("Data Source=:memory:");connection.Open();
  await using AppDb db=sql is null
   ? new SqliteAppDb(new DbContextOptionsBuilder<SqliteAppDb>().UseSqlite(connection).Options)
   : new SqlServerAppDb(new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sql){InitialCatalog="ServiceConnectCatalog_"+Guid.NewGuid().ToString("N")}.ConnectionString).Options);
  try {
   await db.Database.MigrateAsync();await DatabaseSetup.Catalog(db);
   var document=CatalogConfiguration.Read();Assert.True(document.LocationNeutral);Assert.Equal(69,document.SourceCoverage.Length);
   Assert.Equal(34,await db.Services.CountAsync());Assert.Equal(227,await db.ServiceOptions.CountAsync());
   foreach(var source in document.SourceCoverage) {
    var category=document.Categories.Single(c=>c.Key==source.CategoryKey);
    Assert.True(await db.Services.AnyAsync(c=>c.Id==category.Id));Assert.True(await db.ServiceOptions.AnyAsync(o=>o.ServiceId==category.Id));
   }
   var customer=new Account{Email="catalog@example.test",Phone="+12025550188"};db.Accounts.Add(customer);
   var request=new ServiceRequest{CustomerId=customer.Id,ServiceId=DatabaseSetup.GeneralServiceId,ServiceOptionId=DatabaseSetup.HingeRepairOptionId,Title="Existing test request"};db.Requests.Add(request);await db.SaveChangesAsync();
   await DatabaseSetup.Catalog(db);
   Assert.Equal(34,await db.Services.CountAsync());Assert.Equal(227,await db.ServiceOptions.CountAsync());
   Assert.Equal(DatabaseSetup.HingeRepairOptionId,(await db.Requests.SingleAsync()).ServiceOptionId);
   Assert.Equal("Existing test request",(await db.Requests.SingleAsync()).Title);
  } finally { if(sql is not null)await db.Database.EnsureDeletedAsync(); }
 }
}
