using System.Text.Json;
using Domain.Interfaces.Rules;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;

namespace Infrastructure.Rules
{
    public class RulesEngineService : IRulesEngineService
    {
        private readonly ILogger<RulesEngineService> _logger;
        private readonly string _rulesFilePath;
        private RulesEngine.RulesEngine? _engine;
        private DateTime _lastLoaded = DateTime.MinValue;
        private static readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(10);
        private readonly SemaphoreSlim _lock = new(1, 1);

        public RulesEngineService(ILogger<RulesEngineService> logger, string rulesFilePath)
        {
            _logger = logger;
            _rulesFilePath = rulesFilePath;
        }

        public async Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, CancellationToken ct = default)
        {
            return await ValidateAsync(ruleSetName, input, null, ct);
        }

        public async Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, object? dbContext, CancellationToken ct = default)
        {
            await EnsureEngineLoadedAsync(ct);

            try
            {
                var parameters = new List<RuleParameter>
                {
                    new RuleParameter("input", input)
                };

                if (dbContext != null)
                {
                    parameters.Add(new RuleParameter("db", dbContext));
                }

                var results = await _engine!.ExecuteAllRulesAsync(ruleSetName, parameters.ToArray());
                var violations = results.Where(r => !r.IsSuccess)
                    .Select(r => r.ExceptionMessage ?? r.Rule.ErrorMessage ?? r.Rule.RuleName)
                    .ToList();

                _logger.LogDebug("RulesEngine '{RuleSet}': {Valid}, Violations: {Count}",
                    ruleSetName, !violations.Any(), violations.Count);

                return new RuleValidationResult(!violations.Any(), violations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RulesEngine error for ruleSet '{RuleSet}'", ruleSetName);
                return new RuleValidationResult(true, new List<string>()); // Fail-open
            }
        }

        public async Task ReloadRulesAsync(CancellationToken ct = default)
        {
            _lastLoaded = DateTime.MinValue;
            await EnsureEngineLoadedAsync(ct);
        }

        private async Task EnsureEngineLoadedAsync(CancellationToken ct)
        {
            if (_engine != null && DateTime.UtcNow - _lastLoaded < _cacheTime) return;

            await _lock.WaitAsync(ct);
            try
            {
                if (_engine != null && DateTime.UtcNow - _lastLoaded < _cacheTime) return;
                var workflows = await LoadWorkflowsFromJsonAsync(ct);
                var settings = new ReSettings { CustomTypes = new[] { typeof(EmployeeStatus) } };
                _engine = new RulesEngine.RulesEngine(workflows.ToArray(), settings);
                _lastLoaded = DateTime.UtcNow;
                _logger.LogInformation("RulesEngine loaded {Count} workflows from {FilePath}", workflows.Count, _rulesFilePath);
            }
            finally { _lock.Release(); }
        }

        private async Task<List<Workflow>> LoadWorkflowsFromJsonAsync(CancellationToken ct)
        {
            if (!File.Exists(_rulesFilePath))
                throw new FileNotFoundException($"Rules file not found: {_rulesFilePath}");

            var json = await File.ReadAllTextAsync(_rulesFilePath, ct);
            var workflows = JsonSerializer.Deserialize<List<Workflow>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return workflows ?? new List<Workflow>();
        }
    }

    public enum EmployeeStatus { Active = 1, Inactive = 2, OnLeave = 3, Terminated = 4 }
}
