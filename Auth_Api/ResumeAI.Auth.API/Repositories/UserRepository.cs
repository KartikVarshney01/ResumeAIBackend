using Microsoft.EntityFrameworkCore;
using ResumeAI.Auth.API.Data;
using ResumeAI.Auth.API.Models;

namespace ResumeAI.Auth.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmail(string email);
    Task<User?> GetById(int userId);
    Task<User> Save(User user);
    Task<User> Update(User user);
    Task<bool> EmailExists(string email);
}

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _ctx;

    public UserRepository(AuthDbContext ctx) => _ctx = ctx;
    
    public async Task<bool> EmailExists(string email) => await _ctx.Users.AnyAsync(u => u.Email == email);

    public async Task<User?> GetByEmail(string email) =>
        await _ctx.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetById(int userId) => 
        await _ctx.Users.FindAsync(userId);

    public async Task<User> Save(User user)
    {
         _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();
        return user;
    }

    public async Task<User> Update(User user)
    {
        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync();
        return user;
    }
}