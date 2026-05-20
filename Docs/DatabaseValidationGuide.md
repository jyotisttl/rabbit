# Rules Engine with Database Validation

## Overview

This implementation extends the Rules Engine to support **database validations** in addition to input parameter validations. You can now write rules that check if data already exists in the database, validate against existing records, or perform any database query as part of the validation logic.

## Key Features

✅ **Input Validation**: Validate request parameters (format, length, required fields, etc.)  
✅ **Database Validation**: Check for duplicates, verify existence, compare with existing data  
✅ **Flexible Context**: Pass database context to rules for complex validations  
✅ **Clean Separation**: Business logic in JSON rules, not hardcoded

---

## Architecture

### 1. **Database Validation Context**

**`IDbValidationContext`** - Interface for database operations in rules
```csharp
public interface IDbValidationContext
{
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UserExistsAsync(string username, string email);
}
```

**`DbValidationContext`** - Implementation using repository pattern
```csharp
public class DbValidationContext : IDbValidationContext
{
    private readonly IUserRepository _userRepository;
    
    public async Task<bool> UsernameExistsAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        return user != null;
    }
    // ... other methods
}
```

### 2. **Updated Rules Engine Interface**

```csharp
public interface IRulesEngineService
{
    // Original method - input validation only
    Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, CancellationToken ct = default);
    
    // New method - input + database validation
    Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, object? dbContext, CancellationToken ct = default);
}
```

### 3. **Enhanced Repository**

Added methods to support existence checks:

```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default);
}
```

---

## Usage Examples

### Example 1: Check for Duplicate Email on User Creation

**rules.json:**
```json
{
  "WorkflowName": "UserCreation",
  "Rules": [
    {
      "RuleName": "EmailNotExists",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "db.EmailExistsAsync(input.Email).Result == false",
      "ErrorMessage": "Email already exists in the system"
    }
  ]
}
```

**Handler:**
```csharp
public async Task<CreateUserWithRulesResponse> Handle(CreateUserWithRulesCommand request, CancellationToken cancellationToken)
{
    // Create database validation context
    var dbContext = new DbValidationContext(_userRepository);

    // Validate using Rules Engine with database context
    var validationResult = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext, cancellationToken);

    if (!validationResult.IsValid)
    {
        return new CreateUserWithRulesResponse
        {
            Success = false,
            ValidationErrors = validationResult.Violations
        };
    }

    // Proceed with user creation...
}
```

### Example 2: Controller with Database Validation

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    var dbContext = new DbValidationContext(_userRepository);
    var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);

    if (!validation.IsValid)
    {
        return BadRequest(new { errors = validation.Violations });
    }

    // Save to database...
    return Ok(new { success = true });
}
```

### Example 3: Multiple Database Validations

**rules.json:**
```json
{
  "WorkflowName": "UserCreation",
  "Rules": [
    {
      "RuleName": "EmailRequired",
      "Expression": "input.Email != null && input.Email.Length > 0",
      "ErrorMessage": "Email is required"
    },
    {
      "RuleName": "EmailNotExists",
      "Expression": "db.EmailExistsAsync(input.Email).Result == false",
      "ErrorMessage": "Email already exists in the system"
    },
    {
      "RuleName": "UsernameNotExists",
      "Expression": "db.UsernameExistsAsync(input.Username).Result == false",
      "ErrorMessage": "Username already exists in the system"
    }
  ]
}
```

---

## Available Database Validation Methods

### Current Methods

| Method | Description | Example Usage |
|--------|-------------|---------------|
| `UsernameExistsAsync(string)` | Check if username exists | `db.UsernameExistsAsync(input.Username).Result == false` |
| `EmailExistsAsync(string)` | Check if email exists | `db.EmailExistsAsync(input.Email).Result == false` |
| `UserExistsAsync(string, string)` | Check if username or email exists | `db.UserExistsAsync(input.Username, input.Email).Result == false` |

### How to Add Custom Methods

1. **Add to Interface** (`IDbValidationContext`):
```csharp
Task<bool> PhoneNumberExistsAsync(string phoneNumber);
Task<User?> GetUserByIdAsync(Guid id);
```

2. **Implement in Context** (`DbValidationContext`):
```csharp
public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
{
    return await _userRepository.PhoneNumberExistsAsync(phoneNumber);
}
```

3. **Use in Rules**:
```json
{
  "RuleName": "PhoneNotExists",
  "Expression": "db.PhoneNumberExistsAsync(input.PhoneNumber).Result == false",
  "ErrorMessage": "Phone number already registered"
}
```

---

## Rule Expression Patterns

### Pattern 1: Simple Existence Check
```json
"Expression": "db.EmailExistsAsync(input.Email).Result == false"
```

### Pattern 2: Conditional Database Check
```json
"Expression": "input.Email == null || db.EmailExistsAsync(input.Email).Result == false"
```

### Pattern 3: Combined Validations
```json
"Expression": "input.Email.Length > 0 && db.EmailExistsAsync(input.Email).Result == false"
```

### Pattern 4: Complex Logic
```json
"Expression": "(input.Email != null && db.EmailExistsAsync(input.Email).Result == false) && (input.Username != null && db.UsernameExistsAsync(input.Username).Result == false)"
```

---

## Best Practices

### ✅ DO

1. **Create context per request**:
   ```csharp
   var dbContext = new DbValidationContext(_userRepository);
   var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
   ```

2. **Use `.Result` for async calls in expressions**:
   ```json
   "Expression": "db.EmailExistsAsync(input.Email).Result == false"
   ```

3. **Check input before database**:
   ```json
   [
     { "RuleName": "EmailRequired", "Expression": "input.Email != null" },
     { "RuleName": "EmailNotExists", "Expression": "db.EmailExistsAsync(input.Email).Result == false" }
   ]
   ```

4. **Keep database methods focused**:
   - Each method should do ONE thing
   - Return simple types (bool, object, list)
   - Use async/await

### ❌ DON'T

1. **Don't skip database context** when validation needs it
2. **Don't create heavy database operations** in validation context
3. **Don't forget null checks** before calling database methods
4. **Don't use synchronous database calls** in the context

---

## Performance Considerations

### Caching
For frequently checked values, consider caching:

```csharp
public class DbValidationContext : IDbValidationContext
{
    private readonly IMemoryCache _cache;
    private readonly IUserRepository _userRepository;
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _cache.GetOrCreateAsync($"email_exists_{email}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        });
    }
}
```

### Batch Validations
For bulk operations, consider batch database queries instead of individual checks.

---

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task CreateUser_WithDuplicateEmail_ShouldFailValidation()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByEmailAsync("test@example.com", default))
            .ReturnsAsync(new User { Email = "test@example.com" });
    
    var dbContext = new DbValidationContext(mockRepo.Object);
    var request = new CreateUserRequest { Email = "test@example.com" };
    
    // Act
    var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("Email already exists", result.Violations);
}
```

---

## Workflow Example

### Before (Input-Only Validation)
```json
{
  "WorkflowName": "UserCreation",
  "Rules": [
    {
      "RuleName": "EmailFormat",
      "Expression": "Regex.IsMatch(input.Email, \"^[^@]+@[^@]+\\.[^@]+$\")",
      "ErrorMessage": "Invalid email format"
    }
  ]
}
```

### After (Input + Database Validation)
```json
{
  "WorkflowName": "UserCreation",
  "Rules": [
    {
      "RuleName": "EmailFormat",
      "Expression": "Regex.IsMatch(input.Email, \"^[^@]+@[^@]+\\.[^@]+$\")",
      "ErrorMessage": "Invalid email format"
    },
    {
      "RuleName": "EmailNotExists",
      "Expression": "db.EmailExistsAsync(input.Email).Result == false",
      "ErrorMessage": "Email already exists in the system"
    },
    {
      "RuleName": "UsernameNotExists",
      "Expression": "db.UsernameExistsAsync(input.Username).Result == false",
      "ErrorMessage": "Username already exists in the system"
    }
  ]
}
```

---

## Summary

This implementation allows you to:
- ✅ Validate input parameters using lambda expressions
- ✅ Check database for duplicates before insert
- ✅ Compare input data with existing database records
- ✅ Build complex validation logic combining input and database checks
- ✅ Keep all validation rules in JSON (externalized business logic)
- ✅ Easily extend with custom database validation methods

All validation logic is now in `rules.json` - no hardcoded validation in controllers or handlers!
