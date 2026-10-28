using System.Data;
using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public class Workflow(AppDb db)
{
    public Task<Estimate> Quote(Guid requestId, Guid providerId, EstimateInput input) =>
        HandleConflicts(() => QuoteCore(requestId, providerId, input));

    public Task Accept(Guid requestId, Guid customerId, AcceptInput input) =>
        HandleConflicts(async () => { await AcceptCore(requestId, customerId, input); return true; });

    // EF's SQL Server execution strategy may wrap error 1205 in multiple exceptions.
    // Translate it only after the failed transaction has been disposed; do not retry
    // an acceptance against a different revision without the customer's review.
    private static async Task<T> HandleConflicts<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (Exception ex) when (IsDeadlock(ex))
        {
            throw new RuleException(409, "Concurrent change. Refresh and retry.");
        }
    }

    internal static bool IsDeadlock(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
            if (current is Microsoft.Data.SqlClient.SqlException { Number: 1205 }) return true;
        return false;
    }

    private async Task<Estimate> QuoteCore(Guid requestId, Guid providerId, EstimateInput input)
    {
        if (input.Amount <= 0 || input.Amount > 9999999999999999.99m || decimal.Round(input.Amount, 2) != input.Amount || string.IsNullOrWhiteSpace(input.Notes) || input.Notes.Length > 4000) throw new RuleException(400, "Enter a positive amount with at most two decimals and notes (1–4000 characters).");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var provider = await db.Accounts.FindAsync(providerId) ?? throw new RuleException(403, "Provider account required.");
        var profile = await db.Providers.FindAsync(providerId);
        if (provider.Role != "Provider" || !provider.EmailVerified || !provider.PhoneVerified || profile?.Status != "Approved")
            throw new RuleException(403, "Verify email and phone and obtain administrator approval before quoting.");
        var r = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId) ?? throw new RuleException(404, "Request not found.");
        if (r.AcceptedEstimateId != null) throw new RuleException(409, "This request already has an accepted estimate.");
        var e = await db.Estimates.SingleOrDefaultAsync(x => x.RequestId == requestId && x.ProviderId == providerId);
        if ((e?.Revision ?? 0) != input.ExpectedRevision) throw new RuleException(409, "Estimate changed. Refresh and try again.");
        if (e == null) { e = new Estimate { RequestId = requestId, ProviderId = providerId }; db.Estimates.Add(e); }
        e.Revision++; e.Amount = input.Amount; e.Notes = input.Notes.Trim(); e.UpdatedAt = DateTime.UtcNow;
        db.Revisions.Add(new EstimateRevision { EstimateId = e.Id, Number = e.Revision, Amount = e.Amount, Notes = e.Notes }); r.Version = Guid.NewGuid();
        Activity(requestId, providerId, e.Revision == 1 ? "EstimateSubmitted" : "EstimateRevised", $"Revision {e.Revision}; USD {e.Amount:0.00}");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return e;
    }
    private async Task AcceptCore(Guid requestId, Guid customerId, AcceptInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId) ?? throw new RuleException(404, "Request not found.");
        if (r.CustomerId != customerId) throw new RuleException(403, "Only the request owner can accept an estimate.");
        if (r.AcceptedEstimateId != null) throw new RuleException(409, "An estimate has already been accepted.");
        var e = await db.Estimates.SingleOrDefaultAsync(x => x.Id == input.EstimateId && x.RequestId == requestId) ?? throw new RuleException(404, "Estimate not found for this request.");
        if (e.Revision != input.ExpectedRevision) throw new RuleException(409, "The provider revised this estimate. Refresh before accepting.");
        var provider = await db.Accounts.FindAsync(e.ProviderId);
        var profile = await db.Providers.FindAsync(e.ProviderId);
        if (provider is null || !provider.EmailVerified || !provider.PhoneVerified || profile?.Status != "Approved")
            throw new RuleException(409, "This provider is no longer eligible. Choose another estimate.");
        r.AcceptedEstimateId = e.Id; r.Status = "Accepted"; r.Version = Guid.NewGuid();
        Activity(requestId, customerId, "EstimateAccepted", $"Accepted revision {e.Revision}; USD {e.Amount:0.00}");
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public Task MarkWorkDone(Guid requestId, Guid providerId) => HandleConflicts(async () =>
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await OwnedJob(requestId, providerId);
        if (r.Status != "Accepted") throw new RuleException(409, "Only an accepted job can be marked ready for completion.");
        r.Status = "AwaitingCustomerReview"; r.Version = Guid.NewGuid();
        Activity(requestId, providerId, "WorkMarkedDone", "Awaiting customer confirmation and required feedback.");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    });

    public Task Complete(Guid requestId, Guid customerId, CustomerReviewInput input) => HandleConflicts(async () =>
    {
        ValidateReview(input.Feedback, input.Overall, input.Quality, input.Communication, input.Punctuality);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await db.Requests.FindAsync(requestId) ?? throw new RuleException(404, "Request not found.");
        if (r.CustomerId != customerId) throw new RuleException(403, "Only the customer who posted this request can confirm completion.");
        if (r.Status != "AwaitingCustomerReview") throw new RuleException(409, "The provider must mark work done before customer confirmation.");
        var e = await db.Estimates.SingleAsync(x => x.Id == r.AcceptedEstimateId);
        db.Reviews.Add(new ServiceReview { RequestId = requestId, ReviewerId = customerId, RevieweeId = e.ProviderId,
            Direction = "CustomerToProvider", Overall = input.Overall, Quality = input.Quality,
            Communication = input.Communication, Punctuality = input.Punctuality, Feedback = input.Feedback.Trim() });
        r.Status = "Completed"; r.CompletedAt = DateTime.UtcNow; r.Version = Guid.NewGuid();
        Activity(requestId, customerId, "Completed", "Customer confirmed completion and submitted feedback.");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    });

    public Task ReviewCustomer(Guid requestId, Guid providerId, ProviderReviewInput input) => HandleConflicts(async () =>
    {
        ValidateReview(input.Feedback, input.Overall, input.Communication, input.Cooperation, input.Readiness);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await OwnedJob(requestId, providerId);
        if (r.Status != "Completed") throw new RuleException(409, "Rate the customer after the service is completed.");
        if (await db.Reviews.AnyAsync(x => x.RequestId == requestId && x.Direction == "ProviderToCustomer"))
            throw new RuleException(409, "Customer feedback has already been submitted.");
        db.Reviews.Add(new ServiceReview { RequestId = requestId, ReviewerId = providerId, RevieweeId = r.CustomerId,
            Direction = "ProviderToCustomer", Overall = input.Overall, Communication = input.Communication,
            Cooperation = input.Cooperation, Readiness = input.Readiness, Feedback = input.Feedback.Trim() });
        r.Version = Guid.NewGuid();
        Activity(requestId, providerId, "CustomerRated", "Provider submitted customer experience feedback.");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return true;
    });

    private async Task<ServiceRequest> OwnedJob(Guid requestId, Guid providerId)
    {
        var r = await db.Requests.FindAsync(requestId) ?? throw new RuleException(404, "Request not found.");
        if (!await db.Estimates.AnyAsync(x => x.Id == r.AcceptedEstimateId && x.ProviderId == providerId))
            throw new RuleException(403, "Only the accepted provider can update this service.");
        return r;
    }
    private static void ValidateReview(string feedback, params int[] ratings)
    {
        if (ratings.Any(x => x < 1 || x > 5) || string.IsNullOrWhiteSpace(feedback) || feedback.Length > 2000)
            throw new RuleException(400, "All ratings must be 1–5 and written feedback is required (1–2000 characters).");
    }
    public void Activity(Guid requestId, Guid actorId, string kind, string details) =>
        db.Activities.Add(new ServiceActivity { RequestId = requestId, ActorId = actorId, Kind = kind, Details = details });
}
