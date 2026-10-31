using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Entities;
using RevenuePilot.Domain.Opportunities;
using RevenuePilot.Infrastructure.Insights;
using RevenuePilot.Infrastructure.Persistence;

namespace RevenuePilot.Infrastructure.Tracking;

public sealed record CreateOpportunityRequest(Guid CustomerId);
public sealed record ChangeStatusRequest(string? Status, decimal? RecoveredValue, string? Note);

public sealed record ActivityDto(DateTime CreatedAtUtc, string? FromStatus, string ToStatus, decimal? RecoveredValue, string? Note);

public sealed record OpportunityDto(
    Guid Id, Guid CustomerId, string CustomerName, string Title, string Reason,
    decimal EstimatedValue, decimal RecoveredValue, string Priority, string Status,
    DateTime CreatedAtUtc, DateTime? ResolvedAtUtc, IReadOnlyList<ActivityDto> Activities);

public sealed record OpportunityListDto(
    IReadOnlyList<OpportunityDto> Opportunities, decimal OpenEstimatedValue, decimal RecoveredValue,
    int OpenCount, int RecoveredCount, int DismissedCount);

public sealed record Outcome<T>(T? Value, string? Error, int Status)
{
    public static Outcome<T> Ok(T value, int status = 200) => new(value, null, status);
    public static Outcome<T> Fail(string error, int status) => new(default, error, status);
}

public static class OpportunityService
{
    public static async Task<OpportunityListDto> ListAsync(AppDbContext db, Guid businessId, CancellationToken ct)
    {
        var opps = await db.Opportunities.AsNoTracking().Where(o => o.BusinessId == businessId).ToListAsync(ct);
        var names = await db.Customers.AsNoTracking().Where(c => c.BusinessId == businessId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var acts = await db.OpportunityActivities.AsNoTracking().Where(a => a.BusinessId == businessId).ToListAsync(ct);
        var actsByOpp = acts.GroupBy(a => a.OpportunityId).ToDictionary(g => g.Key, g => g.ToList());

        var ordered = opps
            .OrderBy(o => OpportunityRules.IsOpen(o.Status) ? 0 : 1)
            .ThenBy(o => o.Priority == "High" ? 0 : 1)
            .ThenByDescending(o => o.EstimatedValue)
            .Select(o => ToDto(o,
                names.GetValueOrDefault(o.CustomerId, "Unknown customer"),
                actsByOpp.GetValueOrDefault(o.Id) ?? new List<OpportunityActivity>()))
            .ToList();

        return new OpportunityListDto(
            ordered,
            opps.Where(o => OpportunityRules.IsOpen(o.Status)).Sum(o => o.EstimatedValue),
            opps.Where(o => o.Status == OpportunityStatus.Recovered).Sum(o => o.RecoveredValue),
            opps.Count(o => OpportunityRules.IsOpen(o.Status)),
            opps.Count(o => o.Status == OpportunityStatus.Recovered),
            opps.Count(o => o.Status == OpportunityStatus.Dismissed));
    }

    /// <summary>
    /// The client sends only a customer id. The estimate, priority and reason are calculated here
    /// from the customer's own risk assessment, so they cannot be set from the browser.
    /// </summary>
    public static async Task<Outcome<OpportunityDto>> CreateAsync(
        AppDbContext db, Business business, Guid customerId, DateTime nowUtc, CancellationToken ct)
    {
        var detail = await CustomerInsightsService.GetDetailAsync(db, business, customerId, nowUtc, ct);
        if (detail is null)
            return Outcome<OpportunityDto>.Fail("Customer not found.", 404);

        var risk = detail.Customer.Risk;
        if (risk is null || risk.Band == "Low")
            return Outcome<OpportunityDto>.Fail("Only customers flagged Medium or High risk can have an opportunity.", 422);

        var alreadyOpen = await db.Opportunities.AnyAsync(o =>
            o.BusinessId == business.Id && o.CustomerId == customerId &&
            (o.Status == OpportunityStatus.New || o.Status == OpportunityStatus.InProgress), ct);
        if (alreadyOpen)
            return Outcome<OpportunityDto>.Fail("This customer already has an open opportunity.", 409);

        var reason = risk.Evidence.Count > 0 ? string.Join("; ", risk.Evidence) : "Buying behaviour has changed.";
        var opp = new Opportunity
        {
            BusinessId = business.Id,
            CustomerId = customerId,
            Title = $"Re-engage {detail.Customer.Name}",
            Reason = reason.Length > 1000 ? reason[..1000] : reason,
            EstimatedValue = risk.EstimatedExposure,
            RecoveredValue = 0m,
            Priority = risk.Band == "High" ? "High" : "Medium",
            Status = OpportunityStatus.New,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };
        var activity = new OpportunityActivity
        {
            BusinessId = business.Id,
            OpportunityId = opp.Id,
            FromStatus = null,
            ToStatus = OpportunityStatus.New.ToString(),
            Note = "Opportunity created from the churn risk assessment.",
            CreatedAt = nowUtc
        };

        db.Opportunities.Add(opp);
        db.OpportunityActivities.Add(activity);
        try
        {
            await db.SaveChangesAsync(ct); // opportunity and history are saved together or not at all
        }
        catch (DbUpdateException)
        {
            // The unique index caught a simultaneous second request for the same customer.
            return Outcome<OpportunityDto>.Fail("This customer already has an open opportunity.", 409);
        }

        return Outcome<OpportunityDto>.Ok(ToDto(opp, detail.Customer.Name, new[] { activity }), 201);
    }

    public static async Task<Outcome<string>> ChangeStatusAsync(
        AppDbContext db, Guid businessId, Guid id, ChangeStatusRequest req, DateTime nowUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Status) ||
            !Enum.GetNames<OpportunityStatus>().Contains(req.Status.Trim(), StringComparer.OrdinalIgnoreCase))
            return Outcome<string>.Fail("Status must be InProgress, Recovered or Dismissed.", 400);

        var target = Enum.Parse<OpportunityStatus>(req.Status.Trim(), true);
        var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        if (note is { Length: > 500 })
            return Outcome<string>.Fail("The note must be 500 characters or fewer.", 400);

        var opp = await db.Opportunities.FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId, ct);
        if (opp is null)
            return Outcome<string>.Fail("Opportunity not found.", 404);

        var check = OpportunityRules.ValidateTransition(opp.Status, target, req.RecoveredValue);
        if (!check.Ok)
            return Outcome<string>.Fail(check.Error!, 422);

        var from = opp.Status;
        opp.Status = target;
        opp.UpdatedAt = nowUtc;
        if (target == OpportunityStatus.Recovered)
            opp.RecoveredValue = req.RecoveredValue!.Value;
        if (!OpportunityRules.IsOpen(target))
            opp.ResolvedAt = nowUtc;

        db.OpportunityActivities.Add(new OpportunityActivity
        {
            BusinessId = businessId,
            OpportunityId = opp.Id,
            FromStatus = from.ToString(),
            ToStatus = target.ToString(),
            RecoveredValue = target == OpportunityStatus.Recovered ? req.RecoveredValue : null,
            Note = note,
            CreatedAt = nowUtc
        });

        await db.SaveChangesAsync(ct);
        return Outcome<string>.Ok(target.ToString());
    }

    private static OpportunityDto ToDto(Opportunity o, string customerName, IEnumerable<OpportunityActivity> acts) =>
        new(o.Id, o.CustomerId, customerName, o.Title, o.Reason, o.EstimatedValue, o.RecoveredValue,
            o.Priority, o.Status.ToString(), o.CreatedAt, o.ResolvedAt,
            acts.OrderByDescending(a => a.CreatedAt)
                .Select(a => new ActivityDto(a.CreatedAt, a.FromStatus, a.ToStatus, a.RecoveredValue, a.Note))
                .ToList());
}