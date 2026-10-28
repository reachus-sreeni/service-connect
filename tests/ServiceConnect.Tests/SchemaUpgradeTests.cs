using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class SchemaUpgradeTests {
 [Fact] public async Task CatalogUpgradePreservesExistingCustomerAndRequest() {
  var sql=Environment.GetEnvironmentVariable("TEST_SQLSERVER");
  var file=Path.Combine(Path.GetTempPath(),"ServiceConnectUpgrade_"+Guid.NewGuid().ToString("N")+".db");
  await using AppDb db=sql is null
   ? new SqliteAppDb(new DbContextOptionsBuilder<SqliteAppDb>().UseSqlite($"Data Source={file};Pooling=False").Options)
   : new SqlServerAppDb(new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(sql){InitialCatalog="ServiceConnectUpgrade_"+Guid.NewGuid().ToString("N")}.ConnectionString).Options);
  try {
   await db.GetService<IMigrator>().MigrateAsync(sql is null?"20261028022537_InitialLocalSchema":"20261028022633_InitialSqlServerSchema");
   var customer=Guid.NewGuid();var request=Guid.NewGuid();var version=Guid.NewGuid();var category=DatabaseSetup.GeneralServiceId;var now=DateTime.UtcNow;
   await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Accounts] ([Id],[Email],[Phone],[Name],[PasswordHash],[Role],[EmailVerified],[PhoneVerified],[Version]) VALUES ({customer},{"legacy@example.test"},{"+12025550199"},{"Existing customer"},{"test-only-hash"},{"Customer"},{true},{true},{version})");
   await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Services] ([Id],[Name],[Description]) VALUES ({category},{"General repairs"},{"Existing category"})");
   await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Requests] ([Id],[CustomerId],[ServiceId],[Title],[Description],[Location],[Status],[CreatedAt],[Version]) VALUES ({request},{customer},{category},{"Keep my existing project"},{"Preserved details"},{"Demo area"},{"Open"},{now},{version})");
   await db.Database.MigrateAsync();await DatabaseSetup.Catalog(db);
   var restored=await db.Requests.SingleAsync();Assert.Equal(request,restored.Id);Assert.Equal("Keep my existing project",restored.Title);Assert.Null(restored.ServiceOptionId);
   Assert.Equal(customer,(await db.Accounts.SingleAsync()).Id);Assert.Equal(227,await db.ServiceOptions.CountAsync());
  } finally { await db.Database.EnsureDeletedAsync(); }
 }
}
