# Microsoft Rules Engine Implementation Guide

## ✅ What Has Been Fixed

1. **Created `ValidationRule` entity** in `Domain\Entities\Rules\ValidationRule.cs`
2. **Added `ValidationRules` DbSet** to `AppDbContext`
3. **Fixed `RulesEngineService`**:
   - Removed duplicate `using Microsoft.Extensions.Logging`
   - Injected `AppDbContext` instead of missing `_uow`
   - Fixed `BuildWorkflowsFromDbAsync()` to use EF Core directly
4. **Registered service** in `Program.cs` DI container
5. **Created example API controller** `RulesController.cs`
6. **Created example MediatR handler** with rules validation

---

## 📊 Database Setup

Run the SQL script in `Database\Migrations\001_CreateValidationRulesTable.sql` to create the table and sample rules.

Or use EF Core migration:
```bash
dotnet ef migrations add AddValidationRulesTable --project Infrastructure --startup-project WebApi
dotnet ef database update --project Infrastructure --startup-project WebApi
```

---

## 🎯 How to Use Rules Engine in Your API

### **1. On INSERT (POST) - Validate Before Creating**

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IRulesEngineService _rulesEngine;
    private readonly IUserRepository _userRepository;

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // ✅ VALIDATE BEFORE INSERT
        var validation = await _rulesEngine.ValidateAsync("UserCreation", request, ct);
        
        if (!validation.IsValid)
        {
            return new CreateUserResponse 
            { 
                Success = false, 
                Errors = validation.Violations 
            };
        }

        // Proceed with insert
        var user = new User { Username = request.Username, Email = request.Email };
        await _userRepository.AddAsync(user, ct);
        
        return new CreateUserResponse { Success = true, UserId = user.Id };
    }
}
```

### **2. On UPDATE (PUT/PATCH) - Validate Before Updating**

```csharp
public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IRulesEngineService _rulesEngine;
    private readonly IUserRepository _userRepository;

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        // ✅ VALIDATE BEFORE UPDATE
        var validation = await _rulesEngine.ValidateAsync("UserUpdate", request, ct);
        
        if (!validation.IsValid)
        {
            return new UpdateUserResponse 
            { 
                Success = false, 
                Errors = validation.Violations 
            };
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null) return new UpdateUserResponse { Success = false };

        user.Username = request.Username;
        user.Email = request.Email;
        await _userRepository.UpdateAsync(user, ct);
        
        return new UpdateUserResponse { Success = true };
    }
}
```

### **3. On FETCH (GET) - Filter Invalid Data**

```csharp
public class GetUsersHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    private readonly IRulesEngineService _rulesEngine;
    private readonly IUserRepository _userRepository;

    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await _userRepository.GetAllAsync(ct);
        
        // ✅ VALIDATE FETCHED DATA (optional - filter out invalid records)
        var validUsers = new List<UserDto>();
        
        foreach (var user in users)
        {
            var validation = await _rulesEngine.ValidateAsync("UserFetch", user, ct);
            if (validation.IsValid)
            {
                validUsers.Add(MapToDto(user));
            }
        }
        
        return validUsers;
    }
}
```

---

## 🌐 API Endpoints

### **Validate Data Against Rules**
```http
POST /api/rules/validate/UserCreation
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "employeeStatus": "Active"
}
```

**Response (Success):**
```json
{
  "isValid": true,
  "message": "Validation passed"
}
```

**Response (Failure):**
```json
{
  "isValid": false,
  "violations": [
    "Email is required",
    "Username must be at least 3 characters"
  ]
}
```

### **Reload Rules from Database**
```http
POST /api/rules/reload
```

---

## 🔧 Adding New Rules (Runtime Configuration)

Insert new rules into the database:

```sql
INSERT INTO "ValidationRules" 
    ("RuleSetName", "RuleName", "Expression", "ErrorMessage", "Priority", "IsActive")
VALUES 
    ('UserCreation', 'AgeCheck', 'input.Age >= 18', 'User must be 18 or older', 5, true);
```

Then reload rules:
```http
POST /api/rules/reload
```

Or wait 10 minutes (cache expires automatically).

---

## 📝 Rule Expression Examples

### **Simple Property Check**
```csharp
"input.Email != null"
"input.Age >= 18"
"input.Status == \"Active\""
```

### **String Operations**
```csharp
"input.Username.Length >= 3 && input.Username.Length <= 50"
"input.Email.Contains(\"@\")"
```

### **Regex Validation**
```csharp
"System.Text.RegularExpressions.Regex.IsMatch(input.Email, @\"^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$\")"
"System.Text.RegularExpressions.Regex.IsMatch(input.Phone, @\"^\\d{10}$\")"
```

### **Enum Validation**
```csharp
"input.EmployeeStatus == Infrastructure.Rules.EmployeeStatus.Active"
```

### **Complex Conditions**
```csharp
"(input.Age >= 18 && input.Country == \"US\") || input.ParentalConsent == true"
```

---

## 🎨 Best Practices

1. **Use RuleSets** to organize rules by context (UserCreation, UserUpdate, etc.)
2. **Set Priority** to control execution order
3. **Cache Rules** - The service caches for 10 minutes (configurable in `_cacheTime`)
4. **Fail-Open Strategy** - On error, validation passes (line 41 in RulesEngineService)
5. **Inject in Handlers** - Use in MediatR handlers for clean separation
6. **Database-Driven** - Update rules without redeploying code

---

## 📦 NuGet Package Required

Make sure you have installed:
```bash
dotnet add package RulesEngine --version 5.0.3
```

Or in your `.csproj`:
```xml
<PackageReference Include="RulesEngine" Version="5.0.3" />
```

---

## ✅ Summary

**YES, you can:**
- ✅ Write API to **fetch data** and validate with rules engine
- ✅ Validate on **INSERT** before saving to database
- ✅ Validate on **UPDATE** before applying changes
- ✅ Filter **fetched data** based on validation rules
- ✅ Manage rules **at runtime** via database
- ✅ Use in **MediatR handlers** or **directly in controllers**

The implementation is **complete and ready to use**! 🚀
