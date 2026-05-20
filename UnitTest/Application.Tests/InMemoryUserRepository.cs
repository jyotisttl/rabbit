using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Application.Tests
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<User>>(_users);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(user);
        }

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(x => x.Username == username);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(x => x.Email == email);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var index = _users.FindIndex(x => x.Id == user.Id);
            if (index >= 0)
            {
                _users[index] = user;
            }
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default)
        {
            var exists = _users.Any(x => x.Username == username || x.Email == email);
            return Task.FromResult(exists);
        }
    }
}
