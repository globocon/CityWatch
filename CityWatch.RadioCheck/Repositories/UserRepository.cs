using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using CityWatch.Data.Models;
using CityWatch.Data;

namespace FortescueWebApp.Repositories
{
    public interface IUserRepository
    {
        Task AddUserAsync(UserDemo user);
        Task<List<UserDemo>> GetAllUsersAsync();
        Task<UserDemo?> GetUserByIdAsync(int id);
        Task<bool> DeleteUserAsync(int id);
    }
    public class UserRepository : IUserRepository
    {
        private readonly CityWatchDbContext _context;

        public UserRepository(CityWatchDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(UserDemo user)
        {
            _context.UsersDemo.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserDemo>> GetAllUsersAsync()
        {
            return await _context.UsersDemo.ToListAsync();
        }

        public async Task<UserDemo?> GetUserByIdAsync(int id)
        {
            return await _context.UsersDemo.FindAsync(id);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var userDemo = await _context.UsersDemo.FindAsync(id);
            if (userDemo == null)
                return false;

            _context.UsersDemo.Remove(userDemo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
