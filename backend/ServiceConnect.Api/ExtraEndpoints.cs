using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;
public static class ExtraEndpoints
{
    private static Guid UserId(ClaimsPrincipal u) => Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/services", async (AppDb db) => await db.Services.AsNoTracking().OrderBy(x => x.Name).ToListAsync()).RequireAuthorization();
        app.MapGet("/api/me", async (AppDb db, ClaimsPrincipal user) => {
            var a = await db.Accounts.FindAsync(UserId(user)) ?? throw new RuleException(404,"Account not found.");
            var profile = await db.Providers.AsNoTracking().SingleOrDefaultAsync(x => x.AccountId == a.Id);
            return Results.Ok(new { a.Id, a.Name, a.Email, a.Phone, a.Role, a.EmailVerified, a.PhoneVerified, provider = profile });
        }).RequireAuthorization();
        app.MapPost("/api/me/verification/{channel}/send", async (string channel, VerificationService service, ClaimsPrincipal user) => Results.Ok(await service.Send(UserId(user),channel))).RequireAuthorization().RequireRateLimiting("auth");
        app.MapPost("/api/me/verification/{channel}/confirm", async (string channel, VerifyCodeInput input, VerificationService service, ClaimsPrincipal user) => {
            await service.Verify(UserId(user),channel,input); return Results.NoContent();
        }).RequireAuthorization().RequireRateLimiting("auth");
        app.MapGet("/api/admin/providers", async (AppDb db) => await db.Providers.AsNoTracking().Select(p => new {
            p.AccountId, p.BusinessName, p.Qualifications, p.Status, p.ReviewNotes,
            Account = db.Accounts.Where(a => a.Id == p.AccountId).Select(a => new {a.Name,a.Email,a.Phone,a.EmailVerified,a.PhoneVerified}).First()
        }).ToListAsync()).RequireAuthorization(p => p.RequireRole("Admin"));
        app.MapPost("/api/admin/providers/{id:guid}/decision", async (Guid id, ProviderDecisionInput input, AppDb db, ClaimsPrincipal user) => {
            if (string.IsNullOrWhiteSpace(input.Notes) || input.Notes.Length > 2000) throw new RuleException(400,"Review notes are required (1–2000 characters).");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var profile = await db.Providers.FindAsync(id) ?? throw new RuleException(404,"Provider not found.");
            var a = await db.Accounts.SingleAsync(x => x.Id == id);
            if (input.Approve && (!a.EmailVerified || !a.PhoneVerified)) throw new RuleException(409,"Provider must verify email and phone before approval.");
            profile.Status = input.Approve ? "Approved" : "Rejected"; profile.ReviewNotes = input.Notes.Trim();
            profile.ReviewedBy = UserId(user); profile.ReviewedAt = DateTime.UtcNow; profile.Version = Guid.NewGuid();
            db.ProviderDecisions.Add(new ProviderVerificationDecision { ProviderId = id, AdminId = UserId(user), Status = profile.Status, Notes = profile.ReviewNotes });
            await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Admin"));
        app.MapPost("/api/requests/{id:guid}/work-done", async (Guid id, Workflow workflow, ClaimsPrincipal user) => {
            await workflow.MarkWorkDone(id,UserId(user)); return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Provider"));
        app.MapPost("/api/requests/{id:guid}/complete", async (Guid id, CustomerReviewInput input, Workflow workflow, ClaimsPrincipal user) => {
            await workflow.Complete(id,UserId(user),input); return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Customer"));
        app.MapPost("/api/requests/{id:guid}/customer-review", async (Guid id, ProviderReviewInput input, Workflow workflow, ClaimsPrincipal user) => {
            await workflow.ReviewCustomer(id,UserId(user),input); return Results.NoContent();
        }).RequireAuthorization(p => p.RequireRole("Provider"));
        app.MapGet("/api/requests/{id:guid}/history", async (Guid id, AppDb db, ClaimsPrincipal user) => {
            var request = await db.Requests.FindAsync(id) ?? throw new RuleException(404,"Request not found.");
            var uid = UserId(user);
            var acceptedProvider = await db.Estimates.AnyAsync(x => x.Id == request.AcceptedEstimateId && x.ProviderId == uid);
            if (!user.IsInRole("Admin") && request.CustomerId != uid && !acceptedProvider) throw new RuleException(403,"History is visible only to the customer, accepted provider and administrator.");
            return Results.Ok(new {
                activities = await db.Activities.AsNoTracking().Where(x => x.RequestId == id).OrderBy(x => x.CreatedAt).ToListAsync(),
                reviews = await db.Reviews.AsNoTracking().Where(x => x.RequestId == id).OrderBy(x => x.CreatedAt).ToListAsync()
            });
        }).RequireAuthorization();
    }
}
