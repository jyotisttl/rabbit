using Domain.Interfaces;
using Domain.Interfaces.Rules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Admin.Handlers.Users
{
    public class CreateUserWithRulesCommand : IRequest<CreateUserWithRulesResponse>
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeStatus { get; set; } = string.Empty;
    }

    public class CreateUserWithRulesResponse
    {
        public bool Success { get; set; }
        public int? UserId { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class CreateUserWithRulesHandler : IRequestHandler<CreateUserWithRulesCommand, CreateUserWithRulesResponse>
    {
        private readonly IRulesEngineService _rulesEngine;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserWithRulesHandler> _logger;

        public CreateUserWithRulesHandler(
            IRulesEngineService rulesEngine,
            IUserRepository userRepository,
            ILogger<CreateUserWithRulesHandler> logger)
        {
            _rulesEngine = rulesEngine;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<CreateUserWithRulesResponse> Handle(CreateUserWithRulesCommand request, CancellationToken cancellationToken)
        {
            // Create database validation context using factory method
            var dbContext = CreateDbValidationContext(_userRepository);

            // Validate using Rules Engine BEFORE insert (includes database validations)
            var validationResult = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext, cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("User creation validation failed: {Violations}", string.Join(", ", validationResult.Violations));
                return new CreateUserWithRulesResponse
                {
                    Success = false,
                    ValidationErrors = validationResult.Violations
                };
            }

            // TODO: Insert user to database here
            // var user = new User { Username = request.Username, Email = request.Email };
            // await _userRepository.AddAsync(user);

            _logger.LogInformation("User created successfully after rules validation");
            
            return new CreateUserWithRulesResponse
            {
                Success = true,
                UserId = 1 // Replace with actual user ID
            };
        }

        private IDbValidationContext CreateDbValidationContext(IUserRepository userRepository)
        {
            return new DbValidationContextAdapter(userRepository);
        }

        // Adapter class to avoid Infrastructure dependency
        private class DbValidationContextAdapter : IDbValidationContext
        {
            private readonly IUserRepository _userRepository;

            public DbValidationContextAdapter(IUserRepository userRepository)
            {
                _userRepository = userRepository;
            }

            public async Task<bool> UsernameExistsAsync(string username)
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                return user != null;
            }

            public async Task<bool> EmailExistsAsync(string email)
            {
                var user = await _userRepository.GetByEmailAsync(email);
                return user != null;
            }

            public async Task<bool> UserExistsAsync(string username, string email)
            {
                return await _userRepository.ExistsAsync(username, email);
            }
        }
    }
}
