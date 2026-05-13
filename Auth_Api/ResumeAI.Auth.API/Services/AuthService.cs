using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ResumeAI.Auth.API.Models;
using ResumeAI.Auth.API.Repositories;
using StackExchange.Redis;

namespace ResumeAI.Auth.API.Services;

public interface IAuthService
{
    Task<User> Register(User user);
    Task<(string AccessToken, string RefreshToken)> Login(string email, string password);
    Task Logout(string token, int userId);
    Task<(string AccessToken, string RefreshToken)> RefreshToken(string refreshToken, int userId);
    Task<User?> GetUserById(int userId);
    Task<User> UpdateProfile(User user);
    Task ChangePassword(int userId, string currentPassword, string newPassword);
    Task UpdateSubscription(int userId, string plan);
    Task DeactivateAccount(int userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;
    private readonly IDatabase _redis;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(IUserRepository repo, IConfiguration config, IConnectionMultiplexer redis)
    {
        _repo = repo;
        _config = config;
        _redis = redis.GetDatabase();
    }

    public async Task<User> Register(User user)
    {
        if (await _repo.EmailExists(user.Email))
            throw new InvalidOperationException("Email already in use");

        user.PasswordHash = _hasher.HashPassword(user, user.PasswordHash);
        user.CreatedAt = DateTime.UtcNow;
        return await _repo.Save(user);
    }

    public async Task<(string AccessToken, string RefreshToken)> Login(string email, string password)
    {
        var user = await _repo.GetByEmail(email)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive)
            throw new ForbiddenException("Account deactivated");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials");

        var accessToken = GenerateJwt(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token in Redis with 7-day TTL
        await _redis.StringSetAsync($"refresh:{user.UserId}", refreshToken, TimeSpan.FromDays(7));
        return (accessToken, refreshToken);
    }

    public async Task Logout(string token, int userId)
    {
        // Blacklist the access token for its remaining lifetime
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwt = jwtHandler.ReadJwtToken(token);
        var remaining = jwt.ValidTo - DateTime.UtcNow;

        if (remaining > TimeSpan.Zero)
            await _redis.StringSetAsync($"blacklist:{token}", "1", remaining);

        // Remove refresh token
        await _redis.KeyDeleteAsync($"refresh:{userId}");
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshToken(string refreshToken, int userId)
    {
        var stored = await _redis.StringGetAsync($"refresh:{userId}");

        if (!stored.HasValue || stored != refreshToken)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var user = await _repo.GetById(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        var newAccess = GenerateJwt(user);
        var newRefresh = GenerateRefreshToken();

        await _redis.StringSetAsync($"refresh:{userId}", newRefresh, TimeSpan.FromDays(7));
        return (newAccess, newRefresh);
    }

    public async Task<User?> GetUserById(int userId) =>
        await _repo.GetById(userId);

    public async Task<User> UpdateProfile(User user) =>
        await _repo.Update(user);

    public async Task ChangePassword(int userId, string currentPassword, string newPassword)
    {
        var user = await _repo.GetById(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (user.Provider != "LOCAL")
            throw new InvalidOperationException("Cannot change password for OAuth accounts");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await _repo.Update(user);
    }

    public async Task UpdateSubscription(int userId, string plan)
    {
        var user = await _repo.GetById(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.SubscriptionPlan = plan.ToUpper();
        await _repo.Update(user);
    }

    public async Task DeactivateAccount(int userId)
    {
        var user = await _repo.GetById(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.IsActive = false;
        await _repo.Update(user);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim("plan",                    user.SubscriptionPlan)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// Custom exception for 403 responses
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}   