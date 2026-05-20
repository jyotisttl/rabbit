# Database Validation Examples

## Example 1: Basic Duplicate Check

### Scenario
Prevent duplicate email and username during user creation.

### Rules (rules.json)
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
      "RuleName": "EmailNotExists",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "db.EmailExistsAsync(input.Email).Result == false",
      "ErrorMessage": "Email already exists in the system"
    },
    {
      "RuleName": "UsernameNotExists",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "db.UsernameExistsAsync(input.Username).Result == false",
      "ErrorMessage": "Username already exists in the system"
    }
  ]
}
```

### Handler Implementation
```csharp
public async Task<CreateUserWithRulesResponse> Handle(CreateUserWithRulesCommand request, CancellationToken cancellationToken)
{
    // Create database validation context
    var dbContext = new DbValidationContextAdapter(_userRepository);

    // Validate - will check both input format AND database duplicates
    var validationResult = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext, cancellationToken);

    if (!validationResult.IsValid)
    {
        return new CreateUserWithRulesResponse
        {
            Success = false,
            ValidationErrors = validationResult.Violations
        };
    }

    // Safe to insert - no duplicates found
    var user = new User 
    { 
        Username = request.Username, 
        Email = request.Email,
        EmployeeStatus = request.EmployeeStatus
    };
    await _userRepository.AddAsync(user);

    return new CreateUserWithRulesResponse { Success = true };
}
```

---

## Example 2: Update with Duplicate Check (Exclude Current User)

### Scenario
When updating a user, allow them to keep their current email, but prevent them from using another user's email.

### Add to IDbValidationContext Interface
```csharp
public interface IDbValidationContext
{
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EmailExistsForOtherUserAsync(string email, Guid currentUserId);
    Task<bool> UserExistsAsync(string username, string email);
}
```

### Implement in DbValidationContext
```csharp
public class DbValidationContext : IDbValidationContext
{
    private readonly IUserRepository _userRepository;

    public async Task<bool> EmailExistsForOtherUserAsync(string email, Guid currentUserId)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user != null && user.Id != currentUserId;
    }
}
```

### Rules (rules.json)
```json
{
  "WorkflowName": "UserUpdate",
  "Rules": [
    {
      "RuleName": "EmailNotExistsForOtherUser",
      "RuleExpressionType": "LambdaExpression",
      "Expression": "input.Email == null || db.EmailExistsForOtherUserAsync(input.Email, input.UserId).Result == false",
      "ErrorMessage": "Email already exists for another user"
    }
  ]
}
```

---

## Example 3: Complex Business Rules

### Scenario
Only allow managers to have "Terminated" status if they have no active subordinates.

### Add to IDbValidationContext
```csharp
public interface IDbValidationContext
{
    Task<bool> HasActiveSubordinatesAsync(Guid managerId);
    Task<bool> IsManagerAsync(Guid userId);
}
```

### Implement
```csharp
public class DbValidationContext : IDbValidationContext
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public async Task<bool> HasActiveSubordinatesAsync(Guid managerId)
    {
        var subordinates = await _employeeRepository.GetSubordinatesByManagerIdAsync(managerId);
        return subordinates.Any(s => s.Status == "Active");
    }

    public async Task<bool> IsManagerAsync(Guid userId)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId);
        return employee?.IsManager ?? false;
    }
}
```

### Rules
```json
{
  "RuleName": "ManagerWithActiveSubordinatesCantBeTerminated",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "input.EmployeeStatus != \"Terminated\" || (db.IsManagerAsync(input.UserId).Result == false || db.HasActiveSubordinatesAsync(input.UserId).Result == false)",
  "ErrorMessage": "Managers with active subordinates cannot be terminated"
}
```

---

## Example 4: Conditional Database Validation

### Scenario
Only validate email uniqueness if the email is being changed.

### Command with Original Data
```csharp
public class UpdateUserCommand
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? OriginalEmail { get; set; }  // Current email from DB
}
```

### Rules
```json
{
  "RuleName": "EmailNotExistsIfChanged",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "input.Email == null || input.Email == input.OriginalEmail || db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists in the system"
}
```

---

## Example 5: Multi-Table Validation

### Scenario
Validate that a user's department exists before assignment.

### Add Repository
```csharp
public interface IDepartmentRepository
{
    Task<bool> ExistsAsync(Guid departmentId, CancellationToken ct = default);
}
```

### Extend DbValidationContext
```csharp
public class DbValidationContext : IDbValidationContext
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public async Task<bool> DepartmentExistsAsync(Guid departmentId)
    {
        return await _departmentRepository.ExistsAsync(departmentId);
    }
}
```

### Rules
```json
{
  "RuleName": "DepartmentMustExist",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "input.DepartmentId == null || db.DepartmentExistsAsync(input.DepartmentId).Result == true",
  "ErrorMessage": "Department does not exist"
}
```

---

## Example 6: Age-Based Validation with Database Lookup

### Scenario
User must be at least 18 years old based on birth date stored in database.

### Add to IDbValidationContext
```csharp
Task<DateTime?> GetUserBirthDateAsync(Guid userId);
```

### Implementation
```csharp
public async Task<DateTime?> GetUserBirthDateAsync(Guid userId)
{
    var user = await _userRepository.GetByIdAsync(userId);
    return user?.BirthDate;
}
```

### Rules
```json
{
  "RuleName": "UserMustBe18OrOlder",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "db.GetUserBirthDateAsync(input.UserId).Result != null && (DateTime.UtcNow - db.GetUserBirthDateAsync(input.UserId).Result.Value).TotalDays >= 6570",
  "ErrorMessage": "User must be at least 18 years old"
}
```

---

## Example 7: Rate Limiting via Database

### Scenario
Prevent users from creating more than 5 posts per day.

### Add to IDbValidationContext
```csharp
Task<int> GetUserPostCountTodayAsync(Guid userId);
```

### Implementation
```csharp
public async Task<int> GetUserPostCountTodayAsync(Guid userId)
{
    var today = DateTime.UtcNow.Date;
    return await _postRepository.CountByUserAndDateAsync(userId, today);
}
```

### Rules
```json
{
  "RuleName": "DailyPostLimitNotExceeded",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "db.GetUserPostCountTodayAsync(input.UserId).Result < 5",
  "ErrorMessage": "Daily post limit of 5 has been reached"
}
```

---

## Example 8: Hierarchical Validation

### Scenario
User can only assign a role if they have permission level >= the role's required level.

### Add to IDbValidationContext
```csharp
Task<int> GetUserPermissionLevelAsync(Guid userId);
Task<int> GetRoleRequiredLevelAsync(Guid roleId);
```

### Rules
```json
{
  "RuleName": "UserHasPermissionForRole",
  "RuleExpressionType": "LambdaExpression",
  "Expression": "db.GetUserPermissionLevelAsync(input.AssignerId).Result >= db.GetRoleRequiredLevelAsync(input.RoleId).Result",
  "ErrorMessage": "Insufficient permission level to assign this role"
}
```

---

## Testing Database Validations

### Unit Test with Mock Repository
```csharp
[Fact]
public async Task CreateUser_WithDuplicateEmail_ShouldFailValidation()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByEmailAsync("existing@example.com", default))
            .ReturnsAsync(new User { Email = "existing@example.com" });
    
    var dbContext = new DbValidationContextAdapter(mockRepo.Object);
    var request = new CreateUserCommand 
    { 
        Username = "newuser", 
        Email = "existing@example.com" 
    };
    
    // Act
    var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("Email already exists", result.Violations);
}

[Fact]
public async Task CreateUser_WithUniqueEmail_ShouldPassValidation()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByEmailAsync("new@example.com", default))
            .ReturnsAsync((User?)null);
    
    var dbContext = new DbValidationContextAdapter(mockRepo.Object);
    var request = new CreateUserCommand 
    { 
        Username = "newuser", 
        Email = "new@example.com" 
    };
    
    // Act
    var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
    
    // Assert
    Assert.True(result.IsValid);
    Assert.Empty(result.Violations);
}
```

---

## Performance Tips

### 1. Use Specific Queries
```csharp
// ❌ Bad - loads entire user object
public async Task<bool> EmailExistsAsync(string email)
{
    var user = await _userRepository.GetByEmailAsync(email);
    return user != null;
}

// ✅ Good - only checks existence
public async Task<bool> EmailExistsAsync(string email)
{
    return await _context.Users.AnyAsync(u => u.Email == email);
}
```

### 2. Batch Database Calls
```csharp
// ❌ Bad - multiple database calls
{
  "Expression": "db.UsernameExistsAsync(input.Username).Result == false && db.EmailExistsAsync(input.Email).Result == false"
}

// ✅ Good - single database call
public async Task<bool> UserExistsAsync(string username, string email)
{
    return await _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
}

{
  "Expression": "db.UserExistsAsync(input.Username, input.Email).Result == false"
}
```

### 3. Add Caching for Lookups
```csharp
public class CachedDbValidationContext : IDbValidationContext
{
    private readonly IMemoryCache _cache;
    private readonly IDbValidationContext _inner;

    public async Task<bool> DepartmentExistsAsync(Guid departmentId)
    {
        var cacheKey = $"dept_exists_{departmentId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _inner.DepartmentExistsAsync(departmentId);
        });
    }
}
```

---

## Summary

Database validation in rules allows you to:

✅ Check for duplicates before insert  
✅ Validate updates against current database state  
✅ Enforce complex business rules involving multiple tables  
✅ Implement rate limiting and quotas  
✅ Verify referential integrity  
✅ Keep all validation logic in JSON rules (not hardcoded)

All without sacrificing clean architecture or testability!
