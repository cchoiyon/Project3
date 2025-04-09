using Project3.Models.Domain;
using System.Threading.Tasks;

namespace Project3.Services
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> ValidateSecurityAnswersAsync(int userId, string answer1, string answer2, string answer3);
    }
} 