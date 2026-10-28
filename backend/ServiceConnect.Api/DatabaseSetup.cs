using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace ServiceConnect;

public static class DatabaseSetup
{
    public static readonly Guid GeneralServiceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid HingeRepairOptionId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static async Task Catalog(AppDb db)
    {
        var catalog = new[] {
            ("Carpentry", "Furniture, doors and everyday woodwork", true, new[] {
                ("Door & hinge repair", "Repair loose hinges, sticking doors or cabinet doors."),
                ("Furniture assembly", "Assembly of desks, beds and other flat-pack furniture."),
                ("Shelf installation", "Mount and secure shelves in your home or office."),
                ("Cabinet repair", "Adjust drawers, replace handles and repair cabinet fittings."),
                ("Custom woodwork consultation", "Describe a custom project and request an assessment.") }),
            ("Plumbing", "Leaks, fixtures and drainage", true, new[] {
                ("Leak repair", "Assess and repair leaking taps, pipes or connections."),
                ("Drain unblocking", "Help with a slow or blocked sink, shower or drain."),
                ("Tap & fixture installation", "Install or replace taps, shower fittings and fixtures."),
                ("Toilet repair", "Repair flush mechanisms, leaks or loose fittings."),
                ("Plumbing inspection", "Request an assessment when the cause is unclear.") }),
            ("Electrical", "Lighting and electrical maintenance", false, new[] {
                ("Lighting installation", "Install or replace light fixtures."),
                ("Electrical inspection", "Describe an electrical issue for a qualified provider.") }),
            ("Cleaning", "A fresh start for every space", true, new[] {
                ("Home deep cleaning", "Detailed cleaning of living spaces, surfaces and fixtures."),
                ("Kitchen cleaning", "Cleaning of kitchen surfaces, cupboards and appliances."),
                ("Bathroom cleaning", "Deep cleaning of bathroom surfaces and fixtures."),
                ("Move-in / move-out cleaning", "Prepare a property before or after a move."),
                ("Upholstery cleaning", "Describe the sofas, chairs or fabrics that need care.") }),
            ("Gardening", "Healthy gardens and welcoming outdoor spaces", true, new[] {
                ("Lawn care", "Mowing, edging and routine lawn maintenance."),
                ("Planting & garden beds", "Planting, soil preparation and garden-bed care."),
                ("Hedge trimming", "Trim and shape hedges and small shrubs."),
                ("Seasonal garden cleanup", "Leaf removal, weeding and seasonal outdoor tidying."),
                ("Irrigation assessment", "Assess watering systems and irrigation problems.") }),
            ("Automotive Service", "Everyday vehicle care and assessment", true, new[] {
                ("Vehicle inspection", "Describe your vehicle and request a condition assessment."),
                ("Battery check & replacement", "Diagnose battery problems and discuss replacement."),
                ("Oil & filter service", "Request routine oil and filter maintenance."),
                ("Interior detailing", "Cleaning and detailing of the vehicle interior."),
                ("Diagnostic assessment", "Share symptoms or warning lights for an initial diagnosis.") })
        };
        for (var i = 0; i < catalog.Length; i++)
        {
            var (name, description, featured, options) = catalog[i];
            var id = Guid.Parse($"10000000-0000-0000-0000-{i+1:D12}");
            var category = await db.Services.FindAsync(id);
            if (category is null) { category = new ServiceCategory { Id = id }; db.Services.Add(category); }
            category.Name = name; category.Description = description; category.IsFeatured = featured;
            for (var j = 0; j < options.Length; j++)
            {
                var optionId = Guid.Parse($"40000000-0000-0000-0000-{i*100+j+1:D12}");
                if (!await db.ServiceOptions.AnyAsync(x => x.Id == optionId)) db.ServiceOptions.Add(new ServiceOption {
                    Id = optionId, ServiceId = id, Name = options[j].Item1, Description = options[j].Item2, SortOrder = j
                });
            }
        }
        await db.SaveChangesAsync();
    }
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
