using Domain.Interfaces;
using Domain.Interfaces.Rules;
using Infrastructure.Rules;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Example: User API with Rules Engine validation on INSERT, UPDATE, and FETCH.
    /// Rules are loaded from WebApi/Rules/rules.json (WorkflowNames: UserCreation, UserUpdate, UserFetch).
    /// Now supports database validations (e.g., checking for duplicate email/username).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersWithRulesController : ControllerBase
    {
        private readonly IRulesEngineService _rulesEngine;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersWithRulesController> _logger;

        public UsersWithRulesController(
            IRulesEngineService rulesEngine,
            IUserRepository userRepository,
            ILogger<UsersWithRulesController> logger)
        {
            _rulesEngine = rulesEngine;
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// CREATE (INSERT) - Validates against "UserCreation" rules in rules.json before creating user
        /// Includes database validation to check for duplicate email/username
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            // Create database validation context
            var dbContext = new DbValidationContext(_userRepository);

            // ✅ VALIDATE WITH RULES ENGINE BEFORE INSERT (includes DB checks)
            // Rules source: WebApi/Rules/rules.json → WorkflowName: "UserCreation"
            var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);

            if (!validation.IsValid)
            {
                _logger.LogWarning("User creation failed validation: {Violations}", 
                    string.Join(", ", validation.Violations));

                return BadRequest(new
                {
                    success = false,
                    errors = validation.Violations
                });
            }

            // TODO: Insert to database here
            //var user = await _userRepository.AddAsync(new User { ... });

            return Ok(new
            {
                success = true,
                userId = 123, // Replace with actual ID
                message = "User created successfully"
            });
        }

        /// <summary>
        /// UPDATE - Validates against "UserUpdate" rules in rules.json before updating user
        /// Includes database validation to check for duplicate email
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            // Create database validation context
            var dbContext = new DbValidationContext(_userRepository);

            // ✅ VALIDATE WITH RULES ENGINE BEFORE UPDATE (includes DB checks)
            // Rules source: WebApi/Rules/rules.json → WorkflowName: "UserUpdate"
            var validation = await _rulesEngine.ValidateAsync("UserUpdate", request, dbContext);

            if (!validation.IsValid)
            {
                _logger.LogWarning("User update failed validation for ID {UserId}: {Violations}",
                    id, string.Join(", ", validation.Violations));

                return BadRequest(new
                {
                    success = false,
                    errors = validation.Violations
                });
            }

            // TODO: Update database here
            // await _userRepository.UpdateAsync(id, request);

            return Ok(new
            {
                success = true,
                message = "User updated successfully"
            });
        }

        /// <summary>
        /// GET - Fetches users and filters out records that fail "UserFetch" rules in rules.json
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            // TODO: Fetch from database
            var users = new List<UserResponse>
            {
                new() { Id = 1, Username = "john_doe", Email = "john@example.com", EmployeeStatus = "Active" },
                new() { Id = 2, Username = "ab", Email = "invalid-email", EmployeeStatus = "Unknown" }
            };

            // ✅ VALIDATE FETCHED DATA USING RULES ENGINE - filters out invalid records
            // Rules source: WebApi/Rules/rules.json → WorkflowName: "UserFetch"
            var validUsers = new List<UserResponse>();

            foreach (var user in users)
            {
                var validation = await _rulesEngine.ValidateAsync("UserFetch", user);
                if (validation.IsValid)
                {
                    validUsers.Add(user);
                }
                else
                {
                    _logger.LogWarning("User ID {UserId} failed validation on fetch: {Violations}",
                        user.Id, string.Join(", ", validation.Violations));
                }
            }

            return Ok(new
            {
                success = true,
                count = validUsers.Count,
                users = validUsers
            });
        }

        /// <summary>
        /// GET by ID - Validates single record against "UserFetch" rules in rules.json before returning
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            // TODO: Fetch from database
            var user = new UserResponse 
            { 
                Id = id, 
                Username = "john_doe", 
                Email = "john@example.com", 
                EmployeeStatus = "Active" 
            };

            // ✅ VALIDATE BEFORE RETURNING
            // Rules source: WebApi/Rules/rules.json → WorkflowName: "UserFetch"
            var validation = await _rulesEngine.ValidateAsync("UserFetch", user);

            if (!validation.IsValid)
            {
                _logger.LogWarning("User ID {UserId} failed validation: {Violations}",
                    id, string.Join(", ", validation.Violations));

                return UnprocessableEntity(new
                {
                    success = false,
                    message = "User data is invalid",
                    errors = validation.Violations
                });
            }

            return Ok(new
            {
                success = true,
                user
            });
        }
    }

    // DTOs
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeStatus { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? EmployeeStatus { get; set; }
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeStatus { get; set; } = string.Empty;
    }
}
