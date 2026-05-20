using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.EFModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Users
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) { _db = db; }


        public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            await _db.Users.AddAsync(user, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }


        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users.ToListAsync(cancellationToken);
        }


        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }


        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
        }


        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }


        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            _db.Users.Update(user);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default)
        {
            return await _db.Users.AnyAsync(x => x.Username == username || x.Email == email, cancellationToken);
        }
    }
}