namespace RevenuePilot.Domain.Entities;

public enum OpportunityStatus { New, InProgress, Recovered, Dismissed }

public class Opportunity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    /// <summary>A forecast. Never counted as revenue.</summary>
    public decimal EstimatedValue { get; set; }
    /// <summary>Only set when a user records a verified outcome.</summary>
    public decimal RecoveredValue { get; set; }
    public string Priority { get; set; } = "Medium";
    public OpportunityStatus Status { get; set; } = OpportunityStatus.New;
    public DateTime CreatedAt { get; set; }   // UTC
    public DateTime UpdatedAt { get; set; }   // UTC
    public DateTime? ResolvedAt { get; set; } // UTC
}

public class OpportunityActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid OpportunityId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public decimal? RecoveredValue { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }   // UTC
}