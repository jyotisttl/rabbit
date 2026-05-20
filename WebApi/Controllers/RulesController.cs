using Domain.Interfaces.Rules;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesController : ControllerBase
    {
        private readonly IRulesEngineService _rulesEngine;
        private readonly ILogger<RulesController> _logger;

        public RulesController(IRulesEngineService rulesEngine, ILogger<RulesController> logger)
        {
            _rulesEngine = rulesEngine;
            _logger = logger;
        }

        [HttpPost("validate/{ruleSetName}")]
        public async Task<IActionResult> ValidateData(string ruleSetName, [FromBody] object data)
        {
            var result = await _rulesEngine.ValidateAsync(ruleSetName, data);
            
            if (!result.IsValid)
            {
                return BadRequest(new 
                { 
                    isValid = false, 
                    violations = result.Violations 
                });
            }

            return Ok(new { isValid = true, message = "Validation passed" });
        }

        [HttpPost("reload")]
        public async Task<IActionResult> ReloadRules()
        {
            await _rulesEngine.ReloadRulesAsync();
            return Ok(new { message = "Rules reloaded successfully" });
        }
    }
}
