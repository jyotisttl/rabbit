# Complete End-to-End Example - User Registration with Database Validation

This document shows a complete, working example of user registration with database validation to prevent duplicate emails and usernames.

---

## Scenario
Build a user registration endpoint that validates:
1. Email format is valid
2. Email doesn't already exist in database
3. Username is at least 3 characters
4. Username doesn't already exist in database
5. Employee status is valid

---

## Step 1: Define the Rules (rules.json)

**File: `WebApi/Rules/rules.json`**

```json
{
  "WorkflowName": "UserCreation",
  "Rules": [
    {
      "RuleName": "EmailRequired",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "input.Email != null && input.Email.Length > 0",
      "ErrorMessage": "Email is required"
    },
    {
      "RuleName": "EmailFormat",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "System.Text.RegularExpressions.Regex.IsMatch(input.Email, \"^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$\")",
      "ErrorMessage": "Invalid email format"
    },
    {
      "RuleName": "EmailNotExists",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "db.EmailExistsAsync(input.Email).Result == false",
      "ErrorMessage": "Email already exists in the system"
    },
    {
      "RuleName": "UsernameRequired",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "input.Username != null && input.Username.Length >= 3",
      "ErrorMessage": "Username must be at least 3 characters"
    },
    {
      "RuleName": "UsernameNotExists",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "db.UsernameExistsAsync(input.Username).Result == false",
      "ErrorMessage": "Username already exists in the system"
    },
    {
      "RuleName": "EmployeeStatusValid",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "input.EmployeeStatus == \"Active\" || input.EmployeeStatus == \"Inactive\" || input.EmployeeStatus == \"OnLeave\" || input.EmployeeStatus == \"Terminated\"",
      "ErrorMessage": "Employee status must be Active, Inactive, OnLeave, or Terminated"
    }
  ]
}
```

---

## Step 2: Request DTO

**File: `WebApi/Controllers/UsersWithRulesController.cs`**

```csharp
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmployeeStatus { get; set; } = string.Empty;
}
```

---

## Step 3: Controller Implementation

**File: `WebApi/Controllers/UsersWithRulesController.cs`**

```csharp
using Domain.Interfaces;
using Domain.Interfaces.Rules;
using Infrastructure.Rules;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
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

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            // Create database validation context
            var dbContext = new DbValidationContext(_userRepository);

            // Validate using Rules Engine (includes database checks)
            var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);

            if (!validation.IsValid)
            {
                _logger.LogWarning("User creation failed: {Violations}", 
                    string.Join(", ", validation.Violations));

                return BadRequest(new
                {
                    success = false,
                    errors = validation.Violations
                });
            }

            // All validations passed - create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = "hashed_password_here", // TODO: Hash actual password
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            _logger.LogInformation("User created successfully: {UserId}", user.Id);

            return Ok(new
            {
                success = true,
                userId = user.Id,
                message = "User created successfully"
            });
        }
    }
}
```

---

## Step 4: MediatR Handler (Alternative Approach)

**File: `Application/Admin/Handlers/Users/CreateUserWithRulesHandler.cs`**

```csharp
using Domain.Entities;
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
        public Guid? UserId { get; set; }
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

        public async Task<CreateUserWithRulesResponse> Handle(
            CreateUserWithRulesCommand request, 
            CancellationToken cancellationToken)
        {
            // Create database validation context
            var dbContext = new DbValidationContextAdapter(_userRepository);

            // Validate using Rules Engine (includes database checks)
            var validationResult = await _rulesEngine.ValidateAsync(
                "UserCreation", 
                request, 
                dbContext, 
                cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("User creation validation failed: {Violations}", 
                    string.Join(", ", validationResult.Violations));
                
                return new CreateUserWithRulesResponse
                {
                    Success = false,
                    ValidationErrors = validationResult.Violations
                };
            }

            // All validations passed - create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = "hashed_password", // TODO: Hash actual password
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);

            _logger.LogInformation("User created successfully: {UserId}", user.Id);
            
            return new CreateUserWithRulesResponse
            {
                Success = true,
                UserId = user.Id
            };
        }

        // Database validation context adapter (avoids Infrastructure dependency)
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
```

---

## Step 5: Test Scenarios

### Test 1: Valid User Creation

**Request:**
```http
POST /api/userswithbules
Content-Type: application/json

{
  "username": "johndoe",
  "email": "john@example.com",
  "employeeStatus": "Active"
}
```

**Response (Success):**
```json
{
  "success": true,
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "User created successfully"
}
```

**Rules Executed:**
1. ✅ EmailRequired - PASS (email provided)
2. ✅ EmailFormat - PASS (valid format)
3. ✅ EmailNotExists - PASS (email not in database)
4. ✅ UsernameRequired - PASS (username >= 3 chars)
5. ✅ UsernameNotExists - PASS (username not in database)
6. ✅ EmployeeStatusValid - PASS (status is "Active")

---

### Test 2: Duplicate Email

**Request:**
```http
POST /api/userswithbules
Content-Type: application/json

{
  "username": "janedoe",
  "email": "john@example.com",
  "employeeStatus": "Active"
}
```

**Response (Validation Error):**
```json
{
  "success": false,
  "errors": [
    "Email already exists in the system"
  ]
}
```

**Rules Executed:**
1. ✅ EmailRequired - PASS
2. ✅ EmailFormat - PASS
3. ❌ EmailNotExists - FAIL (john@example.com already exists)
4. Validation stops, remaining rules not executed

---

### Test 3: Duplicate Username

**Request:**
```http
POST /api/userswithbules
Content-Type: application/json

{
  "username": "johndoe",
  "email": "jane@example.com",
  "employeeStatus": "Active"
}
```

**Response (Validation Error):**
```json
{
  "success": false,
  "errors": [
    "Username already exists in the system"
  ]
}
```

**Rules Executed:**
1. ✅ EmailRequired - PASS
2. ✅ EmailFormat - PASS
3. ✅ EmailNotExists - PASS (jane@example.com is unique)
4. ✅ UsernameRequired - PASS
5. ❌ UsernameNotExists - FAIL (johndoe already exists)

---

### Test 4: Multiple Validation Errors

**Request:**
```http
POST /api/userswithbules
Content-Type: application/json

{
  "username": "ab",
  "email": "invalid-email",
  "employeeStatus": "Pending"
}
```

**Response (Multiple Errors):**
```json
{
  "success": false,
  "errors": [
    "Invalid email format",
    "Username must be at least 3 characters",
    "Employee status must be Active, Inactive, OnLeave, or Terminated"
  ]
}
```

**Rules Executed:**
1. ✅ EmailRequired - PASS
2. ❌ EmailFormat - FAIL (invalid format)
3. ✅ UsernameRequired - PASS (length check)
4. ❌ UsernameRequired - FAIL (length < 3)
5. ❌ EmployeeStatusValid - FAIL (invalid status)

---

## Step 6: Testing with Postman/cURL

### cURL Command - Success Case
```bash
curl -X POST "https://localhost:5001/api/userswithbules" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "employeeStatus": "Active"
  }'
```

### cURL Command - Duplicate Email
```bash
curl -X POST "https://localhost:5001/api/userswithbules" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "janedoe",
    "email": "john@example.com",
    "employeeStatus": "Active"
  }'
```

---

## Step 7: Unit Testing

**File: `UnitTest/Application.Tests/CreateUserWithRulesHandlerTests.cs`**

```csharp
using Application.Admin.Handlers.Users;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Interfaces.Rules;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTest.Application.Tests
{
    public class CreateUserWithRulesHandlerTests
    {
        private readonly Mock<IRulesEngineService> _rulesEngineMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger<CreateUserWithRulesHandler>> _loggerMock;
        private readonly CreateUserWithRulesHandler _handler;

        public CreateUserWithRulesHandlerTests()
        {
            _rulesEngineMock = new Mock<IRulesEngineService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<CreateUserWithRulesHandler>>();
            
            _handler = new CreateUserWithRulesHandler(
                _rulesEngineMock.Object,
                _userRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_UniqueEmailAndUsername_CreatesUser()
        {
            // Arrange
            var command = new CreateUserWithRulesCommand
            {
                Username = "johndoe",
                Email = "john@example.com",
                EmployeeStatus = "Active"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync("john@example.com", default))
                .ReturnsAsync((User?)null);
            
            _userRepositoryMock.Setup(r => r.GetByUsernameAsync("johndoe", default))
                .ReturnsAsync((User?)null);

            _rulesEngineMock.Setup(r => r.ValidateAsync(
                "UserCreation", 
                It.IsAny<object>(), 
                It.IsAny<object>(), 
                default))
                .ReturnsAsync(new RuleValidationResult(true, new List<string>()));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.UserId);
            Assert.Empty(result.ValidationErrors);
            
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        }

        [Fact]
        public async Task Handle_DuplicateEmail_ReturnsValidationError()
        {
            // Arrange
            var command = new CreateUserWithRulesCommand
            {
                Username = "janedoe",
                Email = "john@example.com",
                EmployeeStatus = "Active"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync("john@example.com", default))
                .ReturnsAsync(new User { Email = "john@example.com" });

            _rulesEngineMock.Setup(r => r.ValidateAsync(
                "UserCreation", 
                It.IsAny<object>(), 
                It.IsAny<object>(), 
                default))
                .ReturnsAsync(new RuleValidationResult(
                    false, 
                    new List<string> { "Email already exists in the system" }));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.UserId);
            Assert.Contains("Email already exists in the system", result.ValidationErrors);
            
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
        }

        [Fact]
        public async Task Handle_DuplicateUsername_ReturnsValidationError()
        {
            // Arrange
            var command = new CreateUserWithRulesCommand
            {
                Username = "johndoe",
                Email = "jane@example.com",
                EmployeeStatus = "Active"
            };

            _userRepositoryMock.Setup(r => r.GetByUsernameAsync("johndoe", default))
                .ReturnsAsync(new User { Username = "johndoe" });

            _rulesEngineMock.Setup(r => r.ValidateAsync(
                "UserCreation", 
                It.IsAny<object>(), 
                It.IsAny<object>(), 
                default))
                .ReturnsAsync(new RuleValidationResult(
                    false, 
                    new List<string> { "Username already exists in the system" }));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.UserId);
            Assert.Contains("Username already exists in the system", result.ValidationErrors);
            
            _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
        }
    }
}
```

---

## Flow Diagram

```
┌─────────────────┐
│ HTTP POST       │
│ /api/users      │
└────────┬────────┘
         │
         ▼
┌─────────────────────┐
│ Controller          │
│ - Receives request  │
│ - Creates DbContext │
└────────┬────────────┘
         │
         ▼
┌─────────────────────────────┐
│ Rules Engine                │
│ ┌─────────────────────────┐ │
│ │ 1. EmailRequired        │ │ ✅ PASS
│ │ 2. EmailFormat          │ │ ✅ PASS
│ │ 3. EmailNotExists       │ │ 🔍 Query Database
│ │    (DB Check)           │ │ ✅ PASS (not found)
│ │ 4. UsernameRequired     │ │ ✅ PASS
│ │ 5. UsernameNotExists    │ │ 🔍 Query Database
│ │    (DB Check)           │ │ ✅ PASS (not found)
│ │ 6. EmployeeStatusValid  │ │ ✅ PASS
│ └─────────────────────────┘ │
└────────┬────────────────────┘
         │ All rules passed
         ▼
┌─────────────────────┐
│ Repository          │
│ - Insert user       │
│ - Save to database  │
└────────┬────────────┘
         │
         ▼
┌─────────────────────┐
│ HTTP 200 OK         │
│ {                   │
│   "success": true,  │
│   "userId": "..."   │
│ }                   │
└─────────────────────┘
```

---

## Summary

This complete example demonstrates:

✅ **Input Validation** - Email format, username length, required fields  
✅ **Database Validation** - Check for duplicate email and username  
✅ **Business Rules** - Valid employee status  
✅ **Error Handling** - Return meaningful validation errors  
✅ **Clean Code** - All rules in JSON, not hardcoded  
✅ **Testable** - Easy to unit test with mocks  

All validation logic is externalized in `rules.json` - no hardcoded checks in your code!
