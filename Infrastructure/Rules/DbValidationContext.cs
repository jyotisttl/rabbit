using Domain.Interfaces;
using Domain.Interfaces.Rules;

namespace Infrastructure.Rules
{
    public class DbValidationContext : IDbValidationContext
    {
        private readonly IUserRepository _userRepository;

        public DbValidationContext(IUserRepository userRepository)
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
