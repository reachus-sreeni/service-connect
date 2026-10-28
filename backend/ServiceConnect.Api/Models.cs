using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class ProviderProfile
{
    public Guid AccountId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Qualifications { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string ReviewNotes { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class ProviderVerificationDecision
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid ProviderId { get; set; }
    public Guid AdminId { get; set; } public string Status { get; set; } = "";
    public string Notes { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class ServiceCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
public class ServiceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? AcceptedEstimateId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class Estimate
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid RequestId { get; set; }
    public Guid ProviderId { get; set; } public int Revision { get; set; }
    public decimal Amount { get; set; } public string Notes { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class EstimateRevision
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid EstimateId { get; set; }
    public int Number { get; set; } public decimal Amount { get; set; }
    public string Notes { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
// One review per direction per service. Customer and provider criteria have explicit names.
public class ServiceReview
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid RequestId { get; set; }
    public Guid ReviewerId { get; set; } public Guid RevieweeId { get; set; }
    public string Direction { get; set; } = ""; public int Overall { get; set; }
    public int Communication { get; set; } public int? Quality { get; set; }
    public int? Punctuality { get; set; } public int? Cooperation { get; set; }
    public int? Readiness { get; set; } public string Feedback { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
// Append-only business activity, not a payment transaction.
public class ServiceActivity
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid RequestId { get; set; }
    public Guid ActorId { get; set; } public string Kind { get; set; } = "";
    public string Details { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class VerificationChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid AccountId { get; set; }
    public string Channel { get; set; } = ""; public string CodeHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } public int Attempts { get; set; }
    public bool Consumed { get; set; } public Guid Version { get; set; } = Guid.NewGuid();
}
public class AppDb(DbContextOptions options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ProviderProfile> Providers => Set<ProviderProfile>();
    public DbSet<ProviderVerificationDecision> ProviderDecisions => Set<ProviderVerificationDecision>();
    public DbSet<ServiceCategory> Services => Set<ServiceCategory>();
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateRevision> Revisions => Set<EstimateRevision>();
    public DbSet<ServiceReview> Reviews => Set<ServiceReview>();
    public DbSet<ServiceActivity> Activities => Set<ServiceActivity>();
    public DbSet<VerificationChallenge> VerificationChallenges => Set<VerificationChallenge>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        m.Entity<Account>().HasIndex(x => x.Phone).IsUnique();
        m.Entity<Account>().Property(x => x.Email).HasMaxLength(254);
        m.Entity<Account>().Property(x => x.Phone).HasMaxLength(16);
        m.Entity<Account>().Property(x => x.Name).HasMaxLength(120);
        m.Entity<Account>().Property(x => x.Role).HasMaxLength(20);
        m.Entity<Account>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<ProviderProfile>().HasKey(x => x.AccountId);
        m.Entity<ProviderProfile>().HasOne<Account>().WithOne().HasForeignKey<ProviderProfile>(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ProviderProfile>().HasOne<Account>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ProviderProfile>().Property(x => x.BusinessName).HasMaxLength(160);
        m.Entity<ProviderProfile>().Property(x => x.Qualifications).HasMaxLength(2000);
        m.Entity<ProviderProfile>().Property(x => x.Status).HasMaxLength(20);
        m.Entity<ProviderProfile>().Property(x => x.ReviewNotes).HasMaxLength(2000);
        m.Entity<ProviderProfile>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<ProviderVerificationDecision>().HasOne<ProviderProfile>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ProviderVerificationDecision>().HasOne<Account>().WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ProviderVerificationDecision>().Property(x => x.Status).HasMaxLength(20);
        m.Entity<ProviderVerificationDecision>().Property(x => x.Notes).HasMaxLength(2000);
        m.Entity<Account>().ToTable(t => t.HasCheckConstraint("CK_Account_Role", "[Role] IN ('Customer','Provider','Admin')"));
        m.Entity<ProviderProfile>().ToTable(t => t.HasCheckConstraint("CK_Provider_Status", "[Status] IN ('Pending','Approved','Rejected')"));
        m.Entity<ServiceRequest>().ToTable(t => t.HasCheckConstraint("CK_Request_Status", "[Status] IN ('Open','Accepted','AwaitingCustomerReview','Completed')"));
        m.Entity<Estimate>().ToTable(t => t.HasCheckConstraint("CK_Estimate_Amount", "[Amount] > 0"));
        m.Entity<ServiceReview>().ToTable(t => {
            t.HasCheckConstraint("CK_Review_Ratings", "[Overall] BETWEEN 1 AND 5 AND [Communication] BETWEEN 1 AND 5 AND ([Quality] IS NULL OR [Quality] BETWEEN 1 AND 5) AND ([Punctuality] IS NULL OR [Punctuality] BETWEEN 1 AND 5) AND ([Cooperation] IS NULL OR [Cooperation] BETWEEN 1 AND 5) AND ([Readiness] IS NULL OR [Readiness] BETWEEN 1 AND 5)");
            t.HasCheckConstraint("CK_Review_Direction", "([Direction] = 'CustomerToProvider' AND [Quality] IS NOT NULL AND [Punctuality] IS NOT NULL AND [Cooperation] IS NULL AND [Readiness] IS NULL) OR ([Direction] = 'ProviderToCustomer' AND [Quality] IS NULL AND [Punctuality] IS NULL AND [Cooperation] IS NOT NULL AND [Readiness] IS NOT NULL)");
            t.HasCheckConstraint("CK_Review_Participants", "[ReviewerId] <> [RevieweeId]");
        });
        m.Entity<ServiceCategory>().HasIndex(x => x.Name).IsUnique();
        m.Entity<ServiceCategory>().Property(x => x.Name).HasMaxLength(100);
        m.Entity<ServiceCategory>().Property(x => x.Description).HasMaxLength(500);
        m.Entity<ServiceRequest>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<ServiceRequest>().Property(x => x.Title).HasMaxLength(120);
        m.Entity<ServiceRequest>().Property(x => x.Description).HasMaxLength(4000);
        m.Entity<ServiceRequest>().Property(x => x.Location).HasMaxLength(200);
        m.Entity<ServiceRequest>().Property(x => x.Status).HasMaxLength(30);
        m.Entity<ServiceRequest>().HasOne<Account>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceRequest>().HasOne<ServiceCategory>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Estimate>().HasIndex(x => new { x.RequestId, x.ProviderId }).IsUnique();
        m.Entity<Estimate>().Property(x => x.Amount).HasPrecision(18, 2);
        m.Entity<Estimate>().Property(x => x.Notes).HasMaxLength(4000);
        m.Entity<Estimate>().HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Estimate>().HasOne<Account>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<EstimateRevision>().HasOne<Estimate>().WithMany().HasForeignKey(x => x.EstimateId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<EstimateRevision>().HasIndex(x => new { x.EstimateId, x.Number }).IsUnique();
        m.Entity<EstimateRevision>().Property(x => x.Amount).HasPrecision(18, 2);
        m.Entity<EstimateRevision>().Property(x => x.Notes).HasMaxLength(4000);
        m.Entity<ServiceReview>().HasIndex(x => new { x.RequestId, x.Direction }).IsUnique();
        m.Entity<ServiceReview>().HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceReview>().HasOne<Account>().WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceReview>().HasOne<Account>().WithMany().HasForeignKey(x => x.RevieweeId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceReview>().Property(x => x.Direction).HasMaxLength(30);
        m.Entity<ServiceReview>().Property(x => x.Feedback).HasMaxLength(2000);
        m.Entity<ServiceActivity>().HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceActivity>().HasOne<Account>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<ServiceActivity>().Property(x => x.Kind).HasMaxLength(50);
        m.Entity<ServiceActivity>().Property(x => x.Details).HasMaxLength(2000);
        m.Entity<ServiceActivity>().HasIndex(x => new { x.RequestId, x.CreatedAt });
        m.Entity<VerificationChallenge>().HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<VerificationChallenge>().Property(x => x.Channel).HasMaxLength(10);
        m.Entity<VerificationChallenge>().Property(x => x.CodeHash).HasMaxLength(64);
        m.Entity<VerificationChallenge>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<VerificationChallenge>().HasIndex(x => new { x.AccountId, x.Channel, x.CreatedAt });
    }
}
public class SqliteAppDb(DbContextOptions<SqliteAppDb> options) : AppDb(options);
public class SqlServerAppDb(DbContextOptions<SqlServerAppDb> options) : AppDb(options);

public record RegisterInput(string Email, string Name, string Password, string Role, string Phone, string? BusinessName, string? Qualifications);
public record LoginInput(string Email, string Password);
public record RequestInput(string Title, string Description, string Location, Guid ServiceId);
public record EstimateInput(decimal Amount, string Notes, int ExpectedRevision);
public record AcceptInput(Guid EstimateId, int ExpectedRevision);
public record CustomerReviewInput(int Overall, int Quality, int Communication, int Punctuality, string Feedback);
public record ProviderReviewInput(int Overall, int Communication, int Cooperation, int Readiness, string Feedback);
public record ProviderDecisionInput(bool Approve, string Notes);
public record VerifyCodeInput(Guid ChallengeId, string Code);
public class RuleException(int status, string message) : Exception(message) { public int Status { get; } = status; }
