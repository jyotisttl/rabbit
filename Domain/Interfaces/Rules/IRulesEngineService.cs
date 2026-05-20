namespace Domain.Interfaces.Rules
{
    public interface IRulesEngineService
    {
        Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, CancellationToken ct = default);
        Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, object? dbContext, CancellationToken ct = default);
        Task ReloadRulesAsync(CancellationToken ct = default);
    }
    public record RuleValidationResult(bool IsValid, List<string> Violations);
}
