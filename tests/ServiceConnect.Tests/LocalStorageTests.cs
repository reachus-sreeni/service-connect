using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class LocalStorageTests {
 [Fact] public async Task FileDatabaseMigratesSeedsAndPersistsWithoutReseeding() {
  var folder=Path.Combine(Path.GetTempPath(),"ServiceConnectTests_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  var file=Path.Combine(folder,"test.db");var options=new DbContextOptionsBuilder<SqliteAppDb>().UseSqlite($"Data Source={file};Pooling=False").Options;
  try {
   int activities;
   await using(var db=new SqliteAppDb(options)) {
    await db.Database.MigrateAsync();await DatabaseSetup.Catalog(db);await DatabaseSetup.Demo(db,new PasswordHasher<Account>(),"Testing-Demo-Password-123");
    Assert.Equal(6,await db.Accounts.CountAsync());Assert.Equal(34,await db.Services.CountAsync());Assert.Equal(3,await db.Providers.CountAsync());
    Assert.Equal(new[]{"Accepted","AwaitingCustomerReview","Completed","Open"},await db.Requests.OrderBy(x=>x.Status).Select(x=>x.Status).ToArrayAsync());
    Assert.Equal(2,await db.Reviews.CountAsync());Assert.True(await db.Estimates.CountAsync()>0);activities=await db.Activities.CountAsync();
    var request=await db.Requests.SingleAsync(x=>x.Status=="Open");request.Title="User test edit";await db.SaveChangesAsync();
   }
   Assert.True(File.Exists(file));
   await using(var db=new SqliteAppDb(options)) {
    await db.Database.MigrateAsync();await DatabaseSetup.Catalog(db);await DatabaseSetup.Demo(db,new PasswordHasher<Account>(),"Another-Password-123");
    Assert.Equal(6,await db.Accounts.CountAsync());Assert.Equal(activities,await db.Activities.CountAsync());Assert.True(await db.Requests.AnyAsync(x=>x.Title=="User test edit"));
    var customer=await db.Accounts.SingleAsync(x=>x.Email=="alex@example.test");Assert.NotEqual(PasswordVerificationResult.Failed,new PasswordHasher<Account>().VerifyHashedPassword(customer,customer.PasswordHash,"Testing-Demo-Password-123"));
   }
  } finally { Directory.Delete(folder,true); }
 }
}
