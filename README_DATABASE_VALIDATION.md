# Database Validation Feature - README

## What's New? 🚀

Your Rules Engine now supports **database validations**! You can now write rules that check if data already exists in the database, compare input against existing records, and enforce complex business rules involving database state.

---

## Quick Start

### Before (Input-Only Validation)
```csharp
var validation = await _rulesEngine.ValidateAsync("UserCreation", request);
```

### After (Input + Database Validation)
```csharp
var dbContext = new DbValidationContext(_userRepository);
var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);
```

That's it! Now your rules can access the database.

---

## Example: Prevent Duplicate Email

### Add Rule to `rules.json`
```json
{
  "RuleName": "EmailNotExists",
  "Expression": "db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists in the system"
}
```

### Use in Code
```csharp
var dbContext = new DbValidationContext(_userRepository);
var validation = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);

if (!validation.IsValid)
{
    // validation.Violations contains: ["Email already exists in the system"]
    return BadRequest(new { errors = validation.Violations });
}

// Safe to insert - email is unique
await _userRepository.AddAsync(new User { Email = request.Email });
```

---

## Available Database Methods

| Method | Usage | Example |
|--------|-------|---------|
| `EmailExistsAsync(string)` | Check if email exists | `db.EmailExistsAsync(input.Email).Result == false` |
| `UsernameExistsAsync(string)` | Check if username exists | `db.UsernameExistsAsync(input.Username).Result == false` |
| `UserExistsAsync(string, string)` | Check if either exists | `db.UserExistsAsync(input.Username, input.Email).Result == false` |

---

## Common Patterns

### Pattern 1: Check for Duplicate
```json
{
  "Expression": "db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists"
}
```

### Pattern 2: Conditional Check (Updates)
```json
{
  "Expression": "input.Email == null || db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists for another user"
}
```

### Pattern 3: Combined Input + Database
```json
{
  "Expression": "input.Email.Length > 0 && db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email is required and must be unique"
}
```

---

## Add Custom Database Validations

### Step 1: Add to Interface
```csharp
// IDbValidationContext.cs
Task<bool> PhoneNumberExistsAsync(string phoneNumber);
```

### Step 2: Implement
```csharp
// DbValidationContext.cs or in your handler's adapter
public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
{
    var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
    return user != null;
}
```

### Step 3: Use in Rules
```json
{
  "Expression": "db.PhoneNumberExistsAsync(input.PhoneNumber).Result == false",
  "ErrorMessage": "Phone number already registered"
}
```

---

## Documentation

📚 **Full Guides:**
- [`Docs/QuickReference.md`](Docs/QuickReference.md) - Quick start guide
- [`Docs/DatabaseValidationGuide.md`](Docs/DatabaseValidationGuide.md) - Architecture and implementation
- [`Docs/DatabaseValidationExamples.md`](Docs/DatabaseValidationExamples.md) - Real-world examples
- [`Docs/CompleteExample.md`](Docs/CompleteExample.md) - End-to-end example with tests
- [`Docs/SUMMARY.md`](Docs/SUMMARY.md) - Complete list of changes

---

## Files Changed

### Created
- ✅ `Domain/Interfaces/Rules/IDbValidationContext.cs` - Database validation interface
- ✅ `Infrastructure/Rules/DbValidationContext.cs` - Implementation
- ✅ `Docs/DatabaseValidationGuide.md` - Comprehensive guide
- ✅ `Docs/DatabaseValidationExamples.md` - Examples
- ✅ `Docs/QuickReference.md` - Quick reference
- ✅ `Docs/CompleteExample.md` - End-to-end example
- ✅ `Docs/SUMMARY.md` - Summary of changes

### Modified
- 📝 `Domain/Interfaces/Rules/IRulesEngineService.cs` - Added dbContext parameter
- 📝 `Domain/Interfaces/Admin/IUserRepository.cs` - Added GetByEmailAsync, ExistsAsync
- 📝 `Infrastructure/Rules/RulesEngineService.cs` - Implemented dbContext support
- 📝 `Infrastructure/Repositories/Admin/Users/UserRepository.cs` - Implemented new methods
- 📝 `Application/Admin/Handlers/Users/CreateUserWithRulesHandler.cs` - Uses dbContext
- 📝 `WebApi/Controllers/UsersWithRulesController.cs` - Uses dbContext
- 📝 `WebApi/Rules/rules.json` - Added database validation rules
- 📝 `UnitTest/Application.Tests/InMemoryUserRepository.cs` - Implemented new methods

---

## Benefits

✅ **Prevent Duplicates** - Check database before insert  
✅ **Externalized Rules** - All validation in JSON, not code  
✅ **Flexible** - Easy to add custom database validations  
✅ **Testable** - Mock repositories in unit tests  
✅ **Clean Architecture** - Proper separation of concerns  
✅ **Backward Compatible** - Existing code still works  

---

## Testing

### Mock Database in Unit Tests
```csharp
var mockRepo = new Mock<IUserRepository>();
mockRepo.Setup(r => r.GetByEmailAsync("test@example.com", default))
        .ReturnsAsync(new User { Email = "test@example.com" });

var dbContext = new DbValidationContextAdapter(mockRepo.Object);
var result = await _rulesEngine.ValidateAsync("UserCreation", request, dbContext);

Assert.False(result.IsValid);
Assert.Contains("Email already exists", result.Violations);
```

---

## Performance Tips

### ✅ Use Batch Queries
```csharp
// Single query for both checks
public async Task<bool> UserExistsAsync(string username, string email)
{
    return await _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
}
```

### ✅ Use Existence Checks (not full loads)
```csharp
// Good
return await _context.Users.AnyAsync(u => u.Email == email);

// Avoid
var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
return user != null;
```

### ✅ Add Caching for Reference Data
```csharp
return await _cache.GetOrCreateAsync($"dept_{id}", async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
    return await _repo.ExistsAsync(id);
});
```

---

## Real-World Use Cases

1. **User Registration** - Prevent duplicate email/username
2. **Product Creation** - Verify category exists
3. **Order Processing** - Check inventory availability
4. **Role Assignment** - Verify user has permission level
5. **Department Assignment** - Verify department exists
6. **Rate Limiting** - Check daily action quota
7. **Hierarchical Rules** - Manager can't be terminated with active subordinates
8. **Conditional Updates** - Allow current value, block others' values

---

## Migration from Old Code

### Old Way (Hardcoded Validation)
```csharp
var existingUser = await _userRepository.GetByEmailAsync(request.Email);
if (existingUser != null)
{
    return BadRequest("Email already exists");
}
```

### New Way (Rules-Based)
```json
{
  "RuleName": "EmailNotExists",
  "Expression": "db.EmailExistsAsync(input.Email).Result == false",
  "ErrorMessage": "Email already exists"
}
```

Benefits:
- ✅ Validation logic in JSON (no code changes to add/modify rules)
- ✅ Consistent error handling
- ✅ Easy to test
- ✅ Centralized validation

---

## FAQ

**Q: Do I need to update my existing code?**  
A: No, it's backward compatible. Existing `ValidateAsync(ruleSetName, input)` still works.

**Q: How do I add custom database validations?**  
A: Add method to `IDbValidationContext`, implement it, and use in rules. See "Add Custom Database Validations" section.

**Q: Can I query multiple tables?**  
A: Yes! Inject multiple repositories into your `DbValidationContext` and create methods for each validation.

**Q: What about performance?**  
A: Use batch queries, existence checks (not full loads), and caching for reference data. See "Performance Tips".

**Q: Can I use this in FluentValidation too?**  
A: Yes! You can call the rules engine from FluentValidation validators.

---

## Get Started

1. **Read** `Docs/QuickReference.md` for quick overview
2. **See** `Docs/CompleteExample.md` for working example
3. **Try** creating a user with duplicate email to see validation in action
4. **Extend** by adding your own database validation methods

---

## Support

For questions or issues:
1. Check the documentation in `Docs/` folder
2. Look at examples in `Docs/DatabaseValidationExamples.md`
3. Review the complete example in `Docs/CompleteExample.md`

---

## What's Next?

Consider:
- Add more database validation methods for your domain
- Implement caching for frequently checked data
- Create entity-specific validation contexts
- Add integration tests with real database
- Monitor performance and optimize queries

Happy coding! 🎉
