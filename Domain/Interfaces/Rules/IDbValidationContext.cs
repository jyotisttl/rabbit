namespace Domain.Interfaces.Rules
{
    public interface IDbValidationContext
    {
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UserExistsAsync(string username, string email);
    }
}
