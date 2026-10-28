using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using ServiceConnect;
using Xunit;
namespace ServiceConnect.Tests;
public class CodeDeliveryTests {
 sealed class TestEnvironment(string name,string root) : IWebHostEnvironment {
  public string ApplicationName {get;set;}="ServiceConnect.Tests";public string EnvironmentName {get;set;}=name;
  public string ContentRootPath {get;set;}=root;public string WebRootPath {get;set;}=root;
  public IFileProvider ContentRootFileProvider {get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider {get;set;}=new NullFileProvider();
 }
 [Theory][InlineData("Development",true)][InlineData("Production",false)]
 public async Task FileOutboxPersistsCodesOnlyInDevelopment(string environment,bool allowed) {
  var folder=Path.Combine(Path.GetTempPath(),"ServiceConnectOutbox_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
  using var services=new ServiceCollection().AddHttpClient().BuildServiceProvider();
  var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Verification:Delivery"]="LocalOutbox"}).Build();
  var delivery=new CodeDelivery(config,new TestEnvironment(environment,folder),services.GetRequiredService<IHttpClientFactory>());var id=Guid.NewGuid();
  try {
   if(!allowed) { await Assert.ThrowsAsync<InvalidOperationException>(()=>delivery.Send("email","demo@example.test","012345",id));Assert.False(Directory.Exists(Path.Combine(folder,".local","outbox")));return; }
   await delivery.Send("email","demo@example.test","012345",id);
   var file=Assert.Single(Directory.GetFiles(Path.Combine(folder,".local","outbox")));var message=JsonDocument.Parse(await File.ReadAllTextAsync(file));
   Assert.Equal("012345",message.RootElement.GetProperty("code").GetString());Assert.Equal(id,message.RootElement.GetProperty("challengeId").GetGuid());
   if(!OperatingSystem.IsWindows())Assert.Equal(UnixFileMode.UserRead|UnixFileMode.UserWrite,File.GetUnixFileMode(file));
  } finally { Directory.Delete(folder,true); }
 }
}
