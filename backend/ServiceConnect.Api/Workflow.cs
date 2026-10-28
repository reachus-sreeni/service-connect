using System.Data;
using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public class Workflow(AppDb db)
{
    public async Task<Estimate> Quote(Guid requestId, Guid providerId, EstimateInput input)
    {
        if (input.Amount <= 0 || input.Amount > 9999999999999999.99m || decimal.Round(input.Amount, 2) != input.Amount || string.IsNullOrWhiteSpace(input.Notes) || input.Notes.Length > 4000) throw new RuleException(400, "Enter a positive amount with at most two decimals and notes (1–4000 characters).");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId) ?? throw new RuleException(404, "Request not found.");
        if (r.AcceptedEstimateId != null) throw new RuleException(409, "This request already has an accepted estimate.");
        var e = await db.Estimates.SingleOrDefaultAsync(x => x.RequestId == requestId && x.ProviderId == providerId);
        if ((e?.Revision ?? 0) != input.ExpectedRevision) throw new RuleException(409, "Estimate changed. Refresh and try again.");
        if (e == null) { e = new Estimate { RequestId = requestId, ProviderId = providerId }; db.Estimates.Add(e); }
        e.Revision++; e.Amount = input.Amount; e.Notes = input.Notes.Trim(); e.UpdatedAt = DateTime.UtcNow;
        db.Revisions.Add(new EstimateRevision { EstimateId = e.Id, Number = e.Revision, Amount = e.Amount, Notes = e.Notes }); r.Version = Guid.NewGuid();
        await db.SaveChangesAsync(); await tx.CommitAsync(); return e;
    }
    public async Task Accept(Guid requestId, Guid customerId, AcceptInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var r = await db.Requests.SingleOrDefaultAsync(x => x.Id == requestId) ?? throw new RuleException(404, "Request not found.");
        if (r.CustomerId != customerId) throw new RuleException(403, "Only the request owner can accept an estimate.");
        if (r.AcceptedEstimateId != null) throw new RuleException(409, "An estimate has already been accepted.");
        var e = await db.Estimates.SingleOrDefaultAsync(x => x.Id == input.EstimateId && x.RequestId == requestId) ?? throw new RuleException(404, "Estimate not found for this request.");
        if (e.Revision != input.ExpectedRevision) throw new RuleException(409, "The provider revised this estimate. Refresh before accepting.");
        r.AcceptedEstimateId = e.Id; r.Version = Guid.NewGuid(); await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
