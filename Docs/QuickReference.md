# Database Validation - Quick Reference

## What Changed?

Your rules engine now supports **database validations** in addition to input parameter validations. You can write rules that check if data already exists in the database.

---

## Key Changes

### 1. **New Interface Method**
```csharp
// IRulesEngineService.cs
Task<RuleValidationResult> ValidateAsync(string ruleSetName, object input, object? dbContext, CancellationToken ct = default);
```

### 2. **New Database Context Interface**
```csharp
// IDbValidationContext.cs
public interface IDbValidationContext
{
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UserExistsAsync(string username, string email);
}
```

### 3. **Updated Repository**
```csharp
// IUserRepository.cs - Added methods:
Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default);
```

### 4. **Updated Rules JSON**
```json
{
  "RuleName": "EmailNotExists",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists in the system"
}
```

---

## How to Use

### In Handlers (MediatR)
```csharp
public async Task<Response> Handle(Command request, CancellationToken ct)
{
    // Create DB validation context adapter
    var dbContext = new DbValidationContextAdapter(_userRepository);
    
    // Validate with database checks
    var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext, ct);
    
    if (!result.IsValid)
    {
        return new Response 
        { 
            Success = false, 
            Errors = result.Violations 
        };
    }
    
    // Proceed with database operation...
}
```

### In Controllers
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
}
```

---

## Available Database Validation Methods

| Method | Usage in Rules | Description |
|--------|----------------|-------------|
| `UsernameExistsAsync(string)` | `db.UsernameExistsAsync(input.Username).Result == false` | Check if username exists |
| `EmailExistsAsync(string)` | `db.EmailExistsAsync(input.Email).Result == false` | Check if email exists |
| `UserExistsAsync(string, string)` | `db.UserExistsAsync(input.Username, input.Email).Result == false` | Check if username or email exists |

---

## Example Rules

### Check for Duplicate Email
```json
{
  "RuleName": "EmailNotExists",
  "Expression": "db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists in the system"
}
```

### Check for Duplicate Username
```json
{
  "RuleName": "UsernameNotExists",
  "Expression": "db.UsernameExistsAsync(input.Username).Result == false",
  "ErrorMessage": "Username already exists in the system"
}
```

### Combined Check (Optimized)
```json
{
  "RuleName": "UserNotExists",
  "Expression": "db.UserExistsAsync(input.Username, input.Email).Result == false",
  "ErrorMessage": "Username or email already exists"
}
```

### Conditional Check (for Updates)
```json
{
  "RuleName": "EmailNotExistsIfProvided",
  "Expression": "input.Email == null || db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists for another user"
}
```

---

## Adding Custom Database Validations

### Step 1: Add Method to Interface
```csharp
// IDbValidationContext.cs
Task<bool> PhoneNumberExistsAsync(string phoneNumber);
```

### Step 2: Implement in Adapter
```csharp
// In your Handler or Controller
private class DbValidationContextAdapter : IDbValidationContext
{
    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        return user != null;
    }
}
```

### Step 3: Use in Rules
```json
{
  "RuleName": "PhoneNotExists",
  "Expression": "db.PhoneNumberExistsAsync(input.PhoneNumber).Result == false",
  "ErrorMessage": "Phone number already registered"
}
```

---

## Files Modified

### Domain Layer
- ✅ `Domain/Interfaces/Rules/IRulesEngineService.cs` - Added overload with dbContext
- ✅ `Domain/Interfaces/Rules/IDbValidationContext.cs` - NEW interface
- ✅ `Domain/Interfaces/Admin/IUserRepository.cs` - Added email/exists methods

### Infrastructure Layer
- ✅ `Infrastructure/Rules/RulesEngineService.cs` - Implemented dbContext support
- ✅ `Infrastructure/Rules/DbValidationContext.cs` - NEW implementation
- ✅ `Infrastructure/Repositories/Admin/Users/UserRepository.cs` - Implemented new methods

### Application Layer
- ✅ `Application/Admin/Handlers/Users/CreateUserWithRulesHandler.cs` - Uses dbContext

### WebApi Layer
- ✅ `WebApi/Controllers/UsersWithRulesController.cs` - Uses dbContext
- ✅ `WebApi/Rules/rules.json` - Added database validation rules

### Documentation
- ✅ `Docs/DatabaseValidationGuide.md` - Comprehensive guide
- ✅ `Docs/DatabaseValidationExamples.md` - Real-world examples
- ✅ `Docs/QuickReference.md` - This file

---

## Testing

### Mock Repository for Unit Tests
```csharp
[Fact]
public async Task CreateUser_DuplicateEmail_FailsValidation()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByEmailAsync("test@example.com", default))
            .ReturnsAsync(new User { Email = "test@example.com" });
    
    var dbContext = new DbValidationContextAdapter(mockRepo.Object);
    
    // Act
    var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("Email already exists", result.Violations);
}
```

---

## Performance Considerations

### ❌ Avoid N+1 Queries
```csharp
// Bad - separate queries
db.UsernameExistsAsync(input.Username).Result == false && 
db.EmailExistsAsync(input.Email).Result == false

// Good - single query
db.UserExistsAsync(input.Username, input.Email).Result == false
```

### ✅ Use Efficient Queries
```csharp
// Bad - loads full entity
var user = await _repo.GetByEmailAsync(email);
return user != null;

// Good - existence check only
return await _context.Users.AnyAsync(u => u.Email == email);
```

### ✅ Add Caching for Lookups
```csharp
public async Task<bool> DepartmentExistsAsync(Guid id)
{
    return await _cache.GetOrCreateAsync($"dept_{id}", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await _deptRepo.ExistsAsync(id);
    });
}
```

---

## Migration Path

### Before (Input-Only Validation)
```csharp
var validation = await _rulesEngine.ValidateAsync("UserCreation", request);
```

### After (Input + Database Validation)
```csharp
var dbContext = new DbValidationContextAdapter(_userRepository);
var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
```

### Backward Compatible
The old method still works for rules that don't need database access:
```csharp
var validation = await _rulesEngine.ValidateAsync("UserCreation", request); // Still works!
```

---

## Summary

🎯 **What You Can Do Now:**
- ✅ Validate input parameters (format, length, required fields)
- ✅ Check for duplicates in database before insert
- ✅ Compare input data with existing database records
- ✅ Enforce complex business rules involving database state
- ✅ Keep ALL validation logic in JSON (no hardcoded checks)

🚀 **Benefits:**
- Centralized validation rules in JSON
- Clean architecture maintained
- Easy to test with mocks
- Extensible for any database validation scenario
- No code changes needed to add/modify rules

📚 **Read More:**
- `Docs/DatabaseValidationGuide.md` - Full implementation guide
- `Docs/DatabaseValidationExamples.md` - Real-world examples
- `WebApi/Rules/rules.json` - See database rules in action
