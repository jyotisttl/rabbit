-- Create ValidationRules table for Microsoft Rules Engine
CREATE TABLE IF NOT EXISTS "ValidationRules" (
    "Id" SERIAL PRIMARY KEY,
    "RuleSetName" VARCHAR(100) NOT NULL,
    "RuleName" VARCHAR(200) NOT NULL,
    "Expression" TEXT NOT NULL,
    "ErrorMessage" VARCHAR(500),
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP
);

-- Create index on RuleSetName and IsActive for better query performance
CREATE INDEX IF NOT EXISTS "IX_ValidationRules_RuleSetName_IsActive" 
ON "ValidationRules" ("RuleSetName", "IsActive");

-- Sample rules for User Creation validation
INSERT INTO "ValidationRules" ("RuleSetName", "RuleName", "Expression", "ErrorMessage", "Priority", "IsActive")
VALUES 
    ('UserCreation', 'EmailRequired', 'input.Email != null && input.Email.Length > 0', 'Email is required', 1, true),
    ('UserCreation', 'EmailFormat', 'System.Text.RegularExpressions.Regex.IsMatch(input.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")', 'Invalid email format', 2, true),
    ('UserCreation', 'UsernameRequired', 'input.Username != null && input.Username.Length >= 3', 'Username must be at least 3 characters', 3, true),
    ('UserCreation', 'UsernameLength', 'input.Username.Length <= 50', 'Username cannot exceed 50 characters', 4, true);

-- Sample rules for User Update validation
INSERT INTO "ValidationRules" ("RuleSetName", "RuleName", "Expression", "ErrorMessage", "Priority", "IsActive")
VALUES 
    ('UserUpdate', 'EmailFormat', 'input.Email == null || System.Text.RegularExpressions.Regex.IsMatch(input.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")', 'Invalid email format', 1, true),
    ('UserUpdate', 'UsernameLength', 'input.Username == null || input.Username.Length <= 50', 'Username cannot exceed 50 characters', 2, true);

-- Sample rules for Employee Status validation
INSERT INTO "ValidationRules" ("RuleSetName", "RuleName", "Expression", "ErrorMessage", "Priority", "IsActive")
VALUES 
    ('EmployeeValidation', 'ValidStatus', 'input.EmployeeStatus == "Active" || input.EmployeeStatus == "Inactive" || input.EmployeeStatus == "OnLeave" || input.EmployeeStatus == "Terminated"', 'Invalid employee status', 1, true);
