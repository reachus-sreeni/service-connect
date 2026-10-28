using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ServiceConnect;

public static class DatabaseSetup
{
    public static readonly Guid GeneralServiceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid HingeRepairOptionId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static Task Catalog(AppDb db) => CatalogConfiguration.Apply(db);
    public static async Task Demo(AppDb db, IPasswordHasher<Account> hasher, string password)
    {
        // Seed once into an empty account store. Restarts never overwrite user testing.
        if (await db.Accounts.AnyAsync()) return;
        await using var tx = await db.Database.BeginTransactionAsync();
        var roles = new[] { "Admin", "Customer", "Customer", "Provider", "Provider", "Provider" };
        var names = new[] { "Demo Administrator", "Alex Customer", "Jamie Customer", "Morgan Repairs", "Taylor Services", "Casey Pending" };
        var emails = new[] { "admin", "alex", "jamie", "morgan", "taylor", "casey" };
        var accounts = roles.Select((role, i) => new Account { Id = Guid.Parse($"20000000-0000-0000-0000-{i+1:D12}"),
            Name = names[i], Email = emails[i] + "@example.test", Phone = "+1202555010" + i, Role = role,
            EmailVerified = i != 5, PhoneVerified = i != 5 }).ToArray();
        foreach (var a in accounts) { a.PasswordHash = hasher.HashPassword(a, password); db.Accounts.Add(a); }
        foreach (var a in accounts.Where(a => a.Role == "Provider")) db.Providers.Add(new ProviderProfile {
            AccountId = a.Id, BusinessName = a.Name, Qualifications = "Demo profile for local testing only.",
            Status = a == accounts[5] ? "Pending" : "Approved", ReviewedBy = a == accounts[5] ? null : accounts[0].Id,
            ReviewedAt = a == accounts[5] ? null : DateTime.UtcNow, ReviewNotes = "Demo approval; not a real identity check." });
        for (var i = 0; i < 4; i++) {
            var r = new ServiceRequest { Id = Guid.Parse($"30000000-0000-0000-0000-{i+1:D12}"), CustomerId = accounts[1].Id,
                ServiceId = GeneralServiceId, ServiceOptionId = HingeRepairOptionId, Title = new[] { "Repair loose cabinet hinge", "Fix garden gate", "Install shelves", "Repair a squeaky door" }[i],
                Description = "Synthetic service request for testing the full lifecycle.", Location = "Demo neighborhood" };
            db.Requests.Add(r);
            db.Activities.Add(new ServiceActivity { RequestId = r.Id, ActorId = accounts[1].Id, Kind = "RequestCreated", Details = "Demo service request." });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        var workflow = new Workflow(db);
        var open = Guid.Parse("30000000-0000-0000-0000-000000000001");
        await workflow.Quote(open, accounts[3].Id, new(90, "Includes labor", 0));
        await workflow.Quote(open, accounts[3].Id, new(110, "Includes replacement hinge", 1));
        await workflow.Quote(open, accounts[4].Id, new(125, "Parts and labor", 0));
        for (var i = 2; i <= 4; i++) {
            var requestId = Guid.Parse($"30000000-0000-0000-0000-{i:D12}");
            var e = await workflow.Quote(requestId, accounts[3].Id, new(100+i*10, "Demo agreed scope", 0));
            await workflow.Accept(requestId, accounts[1].Id, new(e.Id, 1));
            if (i >= 3) await workflow.MarkWorkDone(requestId, accounts[3].Id);
            if (i == 4) {
                await workflow.Complete(requestId, accounts[1].Id, new(5,5,5,4,"Careful repair and clear communication."));
                await workflow.ReviewCustomer(requestId, accounts[3].Id, new(5,5,5,5,"Ready on time with clear requirements."));
            }
        }
    }
    public static async Task CreateAdmin(AppDb db, IConfiguration config, IPasswordHasher<Account> hasher)
    {
        var email = config["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var phone = config["BootstrapAdmin:Phone"]; var password = config["BootstrapAdmin:Password"];
        if (email is null || !System.Net.Mail.MailAddress.TryCreate(email,out _) || phone is null || !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+[1-9]\d{7,14}$") || password is null || password.Length < 12 || password.Length > 128)
            throw new InvalidOperationException("Set BootstrapAdmin__Email, BootstrapAdmin__Phone (international format), BootstrapAdmin__Password (12–128) for --create-admin.");
        if (await db.Accounts.AnyAsync(x => x.Email == email || x.Phone == phone)) throw new InvalidOperationException("Bootstrap administrator already exists; no changes made.");
        var admin = new Account { Name = "Administrator", Email = email, Phone = phone, Role = "Admin" };
        admin.PasswordHash = hasher.HashPassword(admin,password); db.Accounts.Add(admin); await db.SaveChangesAsync();
    }
}
public sealed class SqliteDesignFactory : IDesignTimeDbContextFactory<SqliteAppDb>
{
    public SqliteAppDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteAppDb>().UseSqlite("Data Source=service-connect-design.db").Options);
}
public sealed class SqlServerDesignFactory : IDesignTimeDbContextFactory<SqlServerAppDb>
{
    public SqlServerAppDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerAppDb>().UseSqlServer("Server=localhost;Database=ServiceConnectDesign;Integrated Security=true;TrustServerCertificate=true").Options);
}
