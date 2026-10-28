using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public class Account { public Guid Id { get; set; } = Guid.NewGuid(); public string Email { get; set; } = ""; public string Name { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Role { get; set; } = "Customer"; }
public class ServiceRequest { public Guid Id { get; set; } = Guid.NewGuid(); public Guid CustomerId { get; set; } public string Title { get; set; } = ""; public string Description { get; set; } = ""; public string Location { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid? AcceptedEstimateId { get; set; } public Guid Version { get; set; } = Guid.NewGuid(); }
public class Estimate { public Guid Id { get; set; } = Guid.NewGuid(); public Guid RequestId { get; set; } public Guid ProviderId { get; set; } public int Revision { get; set; } public decimal Amount { get; set; } public string Notes { get; set; } = ""; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; }
public class EstimateRevision { public Guid Id { get; set; } = Guid.NewGuid(); public Guid EstimateId { get; set; } public int Number { get; set; } public decimal Amount { get; set; } public string Notes { get; set; } = ""; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }
public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>(); public DbSet<ServiceRequest> Requests => Set<ServiceRequest>(); public DbSet<Estimate> Estimates => Set<Estimate>(); public DbSet<EstimateRevision> Revisions => Set<EstimateRevision>();
    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Account>().HasIndex(x => x.Email).IsUnique(); m.Entity<Account>().Property(x => x.Email).HasMaxLength(254); m.Entity<Account>().Property(x => x.Name).HasMaxLength(120);
        m.Entity<ServiceRequest>().Property(x => x.Version).IsConcurrencyToken(); m.Entity<ServiceRequest>().HasOne<Account>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Estimate>().HasIndex(x => new { x.RequestId, x.ProviderId }).IsUnique(); m.Entity<Estimate>().Property(x => x.Amount).HasPrecision(18, 2);
        m.Entity<Estimate>().HasOne<ServiceRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict); m.Entity<Estimate>().HasOne<Account>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<EstimateRevision>().HasOne<Estimate>().WithMany().HasForeignKey(x => x.EstimateId).OnDelete(DeleteBehavior.Restrict); m.Entity<EstimateRevision>().HasIndex(x => new { x.EstimateId, x.Number }).IsUnique(); m.Entity<EstimateRevision>().Property(x => x.Amount).HasPrecision(18, 2);
    }
}
public record RegisterInput(string Email, string Name, string Password, string Role);
public record LoginInput(string Email, string Password);
public record RequestInput(string Title, string Description, string Location);
public record EstimateInput(decimal Amount, string Notes, int ExpectedRevision);
public record AcceptInput(Guid EstimateId, int ExpectedRevision);
public class RuleException(int status, string message) : Exception(message) { public int Status { get; } = status; }
