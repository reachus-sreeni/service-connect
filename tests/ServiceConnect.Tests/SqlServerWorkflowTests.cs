using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class SqlServerFactAttribute : FactAttribute {
 public SqlServerFactAttribute() { if (Environment.GetEnvironmentVariable("TEST_SQLSERVER") is null) Skip="Set TEST_SQLSERVER to a disposable SQL Server connection; CI runs this test."; }
}
public class SqlServerWorkflowTests {
 [SqlServerFact] public async Task ConcurrentAcceptanceHasOnlyOneWinner() {
  var cs = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEST_SQLSERVER")) { InitialCatalog="ServiceConnectConcurrency_"+Guid.NewGuid().ToString("N") }.ConnectionString;
  var options=new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer(cs).Options;
  await using var db=new SqlServerAppDb(options);
  try {
   await db.Database.MigrateAsync();
   var c=new Account{Email="customer@example.com",Phone="+12025550101",EmailVerified=true,PhoneVerified=true};var p1=new Account{Email="provider1@example.com",Phone="+12025550102",Role="Provider",EmailVerified=true,PhoneVerified=true};var p2=new Account{Email="provider2@example.com",Phone="+12025550103",Role="Provider",EmailVerified=true,PhoneVerified=true};
   var request=new ServiceRequest{CustomerId=c.Id,ServiceId=DatabaseSetup.GeneralServiceId,Title="Repair"};db.Accounts.AddRange(c,p1,p2);db.Services.Add(new ServiceCategory{Id=DatabaseSetup.GeneralServiceId,Name="General"});db.Providers.AddRange(new ProviderProfile{AccountId=p1.Id,Status="Approved"},new ProviderProfile{AccountId=p2.Id,Status="Approved"});db.Requests.Add(request);await db.SaveChangesAsync();
   var flow=new Workflow(db);var e1=await flow.Quote(request.Id,p1.Id,new(100,"First",0));var e2=await flow.Quote(request.Id,p2.Id,new(120,"Second",0));
   async Task<bool> Accept(Guid id) {
    await using var context=new SqlServerAppDb(options);
    try { await new Workflow(context).Accept(request.Id,c.Id,new(id,1));return true; }
    catch(RuleException ex) when(ex.Status==409) { return false; }
   }
   var results=await Task.WhenAll(Accept(e1.Id),Accept(e2.Id));Assert.Single(results,x=>x);
   db.ChangeTracker.Clear();var accepted=(await db.Requests.SingleAsync()).AcceptedEstimateId;Assert.Contains(accepted,new Guid?[]{e1.Id,e2.Id});
  } finally { await db.Database.EnsureDeletedAsync(); }
 }
}
