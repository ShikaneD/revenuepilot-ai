using RevenuePilot.Domain.Entities;

namespace RevenuePilot.Domain.Opportunities;

public sealed record RuleResult(bool Ok, string? Error)
{
    public static RuleResult Success { get; } = new(true, null);
    public static RuleResult Fail(string error) => new(false, error);
}

/// <summary>
/// Lifecycle: New -> InProgress -> Recovered or Dismissed (New may also go straight to either).
/// Recovered and Dismissed are final. A recovered amount may only be recorded when the
/// status becomes Recovered, and it must be greater than zero.
/// </summary>
public static class OpportunityRules
{
    public const decimal MaxRecoveredValue = 1_000_000_000m;

    public static bool IsOpen(OpportunityStatus status) =>
        status is OpportunityStatus.New or OpportunityStatus.InProgress;

    public static RuleResult ValidateTransition(
        OpportunityStatus current, OpportunityStatus target, decimal? recoveredValue)
    {
        if (!IsOpen(current))
            return RuleResult.Fail("This opportunity is closed and cannot be changed.");
        if (target == current)
            return RuleResult.Fail("The opportunity already has this status.");
        if (target == OpportunityStatus.New)
            return RuleResult.Fail("An opportunity cannot go back to New.");

        if (target == OpportunityStatus.Recovered)
        {
            if (recoveredValue is null || recoveredValue.Value <= 0m)
                return RuleResult.Fail("Enter the amount actually recovered (greater than zero) to mark an opportunity as Recovered.");
            if (recoveredValue.Value > MaxRecoveredValue)
                return RuleResult.Fail("That recovered amount is too large.");
            if (decimal.Round(recoveredValue.Value, 2) != recoveredValue.Value)
                return RuleResult.Fail("The recovered amount can have at most 2 decimal places.");
            return RuleResult.Success;
        }

        if (recoveredValue.HasValue && recoveredValue.Value != 0m)
            return RuleResult.Fail("A recovered amount can only be recorded when the status is Recovered.");
        return RuleResult.Success;
    }
}