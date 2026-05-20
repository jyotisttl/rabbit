using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default);
    }
}
