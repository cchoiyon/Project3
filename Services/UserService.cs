using Project3.Models.Domain;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Project3.Database;

namespace Project3.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ValidateSecurityAnswersAsync(int userId, string answer1, string answer2, string answer3)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            // TODO: Implement actual security answer validation logic
            // This is a placeholder implementation
            return true;
        }
    }
} 