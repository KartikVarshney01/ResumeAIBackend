// using FluentAssertions;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.Extensions.Configuration;
// using Moq;
// using NUnit.Framework;
// using ResumeAI.Auth.API.Models;
// using ResumeAI.Auth.API.Repositories;
// using ResumeAI.Auth.API.Services;
// using StackExchange.Redis;

// namespace ResumeAI.Auth.Tests;

// [TestFixture]
// public class AuthServiceTests
// {
//     private Mock<IUserRepository>     _userRepoMock;
//     private Mock<IConfiguration>      _configMock;
//     private Mock<IConnectionMultiplexer> _redisMock;
//     private Mock<IDatabase>           _redisDatabaseMock;
//     private AuthService               _authService;
//     private PasswordHasher<User>      _hasher;

//     [SetUp]
//     public void SetUp()
//     {
//         _userRepoMock      = new Mock<IUserRepository>();
//         _configMock        = new Mock<IConfiguration>();
//         _redisMock         = new Mock<IConnectionMultiplexer>();
//         _redisDatabaseMock = new Mock<IDatabase>();
//         _hasher            = new PasswordHasher<User>();

//         // Setup JWT config
//         _configMock.Setup(c => c["Jwt:Secret"])
//             .Returns("ThisIsAVeryLongSecretKeyForResumeAIPlatform2026!");
//         _configMock.Setup(c => c["Jwt:Issuer"])
//             .Returns("ResumeAI.Auth");
//         _configMock.Setup(c => c["Jwt:Audience"])
//             .Returns("ResumeAI.Clients");

//         // Setup Redis
//         _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
//             .Returns(_redisDatabaseMock.Object);

//         _authService = new AuthService(
//             _userRepoMock.Object,
//             _configMock.Object,
//             _redisMock.Object);
//     }

//     // ── Register Tests ────────────────────────────────────────────────────────

//     [Test]
//     public async Task Register_WithNewEmail_ShouldCreateUser()
//     {
//         // Arrange
//         var user = new User
//         {
//             FullName     = "Test User",
//             Email        = "test@example.com",
//             PasswordHash = "Test@1234"
//         };

//         _userRepoMock.Setup(r => r.EmailExists(user.Email))
//             .ReturnsAsync(false);

//         _userRepoMock.Setup(r => r.Save(It.IsAny<User>()))
//             .ReturnsAsync((User u) => u);

//         // Act
//         var result = await _authService.Register(user);

//         // Assert
//         result.Should().NotBeNull();
//         result.Email.Should().Be("test@example.com");
//         result.PasswordHash.Should().NotBe("Test@1234"); // should be hashed
//         _userRepoMock.Verify(r => r.Save(It.IsAny<User>()), Times.Once);
//     }

//     [Test]
//     public async Task Register_WithExistingEmail_ShouldThrowInvalidOperation()
//     {
//         // Arrange
//         var user = new User { Email = "existing@example.com", PasswordHash = "Test@1234" };

//         _userRepoMock.Setup(r => r.EmailExists(user.Email))
//             .ReturnsAsync(true);

//         // Act
//         var act = async () => await _authService.Register(user);

//         // Assert
//         await act.Should().ThrowAsync<InvalidOperationException>()
//             .WithMessage("Email already in use");
//     }

//     // ── Login Tests ───────────────────────────────────────────────────────────

//     [Test]
//     public async Task Login_WithValidCredentials_ShouldReturnTokens()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId           = 1,
//             Email            = "test@example.com",
//             PasswordHash     = _hasher.HashPassword(new User(), "Test@1234"),
//             IsActive         = true,
//             Role             = "USER",
//             SubscriptionPlan = "FREE"
//         };

//         _userRepoMock.Setup(r => r.GetByEmail("test@example.com"))
//             .ReturnsAsync(user);

//         _redisDatabaseMock.Setup(r => r.StringSetAsync(
//             It.IsAny<RedisKey>(),
//             It.IsAny<RedisValue>(),
//             It.IsAny<TimeSpan?>(),
//             It.IsAny<bool>(),
//             It.IsAny<When>(),
//             It.IsAny<CommandFlags>()))
//             .ReturnsAsync(true);

//         // Act
//         var (accessToken, refreshToken) = await _authService.Login("test@example.com", "Test@1234");

//         // Assert
//         accessToken.Should().NotBeNullOrEmpty();
//         refreshToken.Should().NotBeNullOrEmpty();
//         accessToken.Split('.').Should().HaveCount(3); // valid JWT format
//     }

//     [Test]
//     public async Task Login_WithWrongPassword_ShouldThrowUnauthorized()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId       = 1,
//             Email        = "test@example.com",
//             PasswordHash = _hasher.HashPassword(new User(), "CorrectPassword"),
//             IsActive     = true
//         };

//         _userRepoMock.Setup(r => r.GetByEmail("test@example.com"))
//             .ReturnsAsync(user);

//         // Act
//         var act = async () => await _authService.Login("test@example.com", "WrongPassword");

//         // Assert
//         await act.Should().ThrowAsync<UnauthorizedAccessException>()
//             .WithMessage("Invalid credentials");
//     }

//     [Test]
//     public async Task Login_WithNonExistentEmail_ShouldThrowUnauthorized()
//     {
//         // Arrange
//         _userRepoMock.Setup(r => r.GetByEmail("noone@example.com"))
//             .ReturnsAsync((User?)null);

//         // Act
//         var act = async () => await _authService.Login("noone@example.com", "Test@1234");

//         // Assert
//         await act.Should().ThrowAsync<UnauthorizedAccessException>()
//             .WithMessage("Invalid credentials");
//     }

//     [Test]
//     public async Task Login_WithDeactivatedAccount_ShouldThrowForbidden()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId       = 1,
//             Email        = "test@example.com",
//             PasswordHash = _hasher.HashPassword(new User(), "Test@1234"),
//             IsActive     = false  // deactivated
//         };

//         _userRepoMock.Setup(r => r.GetByEmail("test@example.com"))
//             .ReturnsAsync(user);

//         // Act
//         var act = async () => await _authService.Login("test@example.com", "Test@1234");

//         // Assert
//         await act.Should().ThrowAsync<ForbiddenException>()
//             .WithMessage("Account deactivated");
//     }

//     // ── ChangePassword Tests ──────────────────────────────────────────────────

//     [Test]
//     public async Task ChangePassword_WithCorrectCurrentPassword_ShouldUpdateHash()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId       = 1,
//             Provider     = "LOCAL",
//             PasswordHash = _hasher.HashPassword(new User(), "OldPassword")
//         };

//         _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
//         _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

//         // Act
//         await _authService.ChangePassword(1, "OldPassword", "NewPassword123");

//         // Assert
//         _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
//             u.PasswordHash != _hasher.HashPassword(new User(), "OldPassword")
//         )), Times.Once);
//     }

//     [Test]
//     public async Task ChangePassword_WithWrongCurrentPassword_ShouldThrowUnauthorized()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId       = 1,
//             Provider     = "LOCAL",
//             PasswordHash = _hasher.HashPassword(new User(), "CorrectPassword")
//         };

//         _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);

//         // Act
//         var act = async () => await _authService.ChangePassword(1, "WrongPassword", "NewPassword");

//         // Assert
//         await act.Should().ThrowAsync<UnauthorizedAccessException>()
//             .WithMessage("Current password is incorrect");
//     }

//     [Test]
//     public async Task ChangePassword_ForOAuthAccount_ShouldThrowInvalidOperation()
//     {
//         // Arrange
//         var user = new User
//         {
//             UserId   = 1,
//             Provider = "GOOGLE" // OAuth account
//         };

//         _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);

//         // Act
//         var act = async () => await _authService.ChangePassword(1, "any", "newpass");

//         // Assert
//         await act.Should().ThrowAsync<InvalidOperationException>()
//             .WithMessage("Cannot change password for OAuth accounts");
//     }

//     // ── UpdateSubscription Tests ──────────────────────────────────────────────

//     [Test]
//     public async Task UpdateSubscription_ShouldUpdatePlanToUpperCase()
//     {
//         // Arrange
//         var user = new User { UserId = 1, SubscriptionPlan = "FREE" };

//         _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
//         _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

//         // Act
//         await _authService.UpdateSubscription(1, "premium");

//         // Assert
//         _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
//             u.SubscriptionPlan == "PREMIUM"
//         )), Times.Once);
//     }

//     // ── DeactivateAccount Tests ───────────────────────────────────────────────

//     [Test]
//     public async Task DeactivateAccount_ShouldSetIsActiveToFalse()
//     {
//         // Arrange
//         var user = new User { UserId = 1, IsActive = true };

//         _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
//         _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

//         // Act
//         await _authService.DeactivateAccount(1);

//         // Assert
//         _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
//             u.IsActive == false
//         )), Times.Once);
//     }

//     [Test]
//     public async Task DeactivateAccount_WithNonExistentUser_ShouldThrowKeyNotFound()
//     {
//         // Arrange
//         _userRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((User?)null);

//         // Act
//         var act = async () => await _authService.DeactivateAccount(99);

//         // Assert
//         await act.Should().ThrowAsync<KeyNotFoundException>()
//             .WithMessage("User not found");
//     }
// }


using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using ResumeAI.Auth.API.Models;
using ResumeAI.Auth.API.Repositories;
using ResumeAI.Auth.API.Services;
using StackExchange.Redis;

namespace ResumeAI.Auth.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository>        _userRepoMock;
    private Mock<IConfiguration>         _configMock;
    private Mock<IConnectionMultiplexer> _redisMock;
    private Mock<IDatabase>              _redisDatabaseMock;
    private AuthService                  _authService;
    private PasswordHasher<User>         _hasher;

    [SetUp]
    public void SetUp()
    {
        _userRepoMock      = new Mock<IUserRepository>();
        _configMock        = new Mock<IConfiguration>();
        _redisMock         = new Mock<IConnectionMultiplexer>();
        _redisDatabaseMock = new Mock<IDatabase>();
        _hasher            = new PasswordHasher<User>();

        _configMock.Setup(c => c["Jwt:Secret"])
            .Returns("ThisIsAVeryLongSecretKeyForResumeAIPlatform2026!");
        _configMock.Setup(c => c["Jwt:Issuer"])
            .Returns("ResumeAI.Auth");
        _configMock.Setup(c => c["Jwt:Audience"])
            .Returns("ResumeAI.Clients");

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _authService = new AuthService(
            _userRepoMock.Object,
            _configMock.Object,
            _redisMock.Object);
    }

    // ── Register Tests ────────────────────────────────────────────────────────

    [Test]
    public async Task Register_WithNewEmail_ShouldCreateUser()
    {
        var user = new User
        {
            FullName     = "Test User",
            Email        = "test@example.com",
            PasswordHash = "Test@1234"
        };

        _userRepoMock.Setup(r => r.EmailExists(user.Email)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.Save(It.IsAny<User>())).ReturnsAsync((User u) => u);

        var result = await _authService.Register(user);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.PasswordHash.Should().NotBe("Test@1234"); // must be hashed
        _userRepoMock.Verify(r => r.Save(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task Register_WithExistingEmail_ShouldThrowInvalidOperation()
    {
        var user = new User { Email = "existing@example.com", PasswordHash = "Test@1234" };

        _userRepoMock.Setup(r => r.EmailExists(user.Email)).ReturnsAsync(true);

        var act = async () => await _authService.Register(user);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already in use");
    }

    // ── Login Tests ───────────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var user = new User
        {
            UserId           = 1,
            Email            = "test@example.com",
            PasswordHash     = _hasher.HashPassword(new User(), "Test@1234"),
            IsActive         = true,
            Role             = "USER",
            SubscriptionPlan = "FREE"
        };

        _userRepoMock.Setup(r => r.GetByEmail("test@example.com")).ReturnsAsync(user);

        _redisDatabaseMock.Setup(r => r.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var (accessToken, refreshToken) = await _authService.Login("test@example.com", "Test@1234");

        accessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();
        accessToken.Split('.').Should().HaveCount(3); // valid JWT
    }

    [Test]
    public async Task Login_WithWrongPassword_ShouldThrowUnauthorized()
    {
        var user = new User
        {
            UserId       = 1,
            Email        = "test@example.com",
            PasswordHash = _hasher.HashPassword(new User(), "CorrectPassword"),
            IsActive     = true
        };

        _userRepoMock.Setup(r => r.GetByEmail("test@example.com")).ReturnsAsync(user);

        var act = async () => await _authService.Login("test@example.com", "WrongPassword");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Test]
    public async Task Login_WithNonExistentEmail_ShouldThrowUnauthorized()
    {
        _userRepoMock.Setup(r => r.GetByEmail("noone@example.com"))
            .ReturnsAsync((User?)null);

        var act = async () => await _authService.Login("noone@example.com", "Test@1234");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Test]
    public async Task Login_WithDeactivatedAccount_ShouldThrowForbidden()
    {
        var user = new User
        {
            UserId       = 1,
            Email        = "test@example.com",
            PasswordHash = _hasher.HashPassword(new User(), "Test@1234"),
            IsActive     = false
        };

        _userRepoMock.Setup(r => r.GetByEmail("test@example.com")).ReturnsAsync(user);

        var act = async () => await _authService.Login("test@example.com", "Test@1234");

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Account deactivated");
    }

    // ── Logout Tests ──────────────────────────────────────────────────────────

    [Test]
    public async Task Logout_WithValidToken_ShouldBlacklistInRedis()
    {
        // Arrange — generate a real JWT first
        var user = new User
        {
            UserId           = 1,
            Email            = "test@example.com",
            PasswordHash     = _hasher.HashPassword(new User(), "Test@1234"),
            IsActive         = true,
            Role             = "USER",
            SubscriptionPlan = "FREE"
        };

        _userRepoMock.Setup(r => r.GetByEmail(user.Email)).ReturnsAsync(user);

        _redisDatabaseMock.Setup(r => r.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _redisDatabaseMock.Setup(r => r.KeyDeleteAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var (accessToken, _) = await _authService.Login(user.Email, "Test@1234");

        // Act
        await _authService.Logout(accessToken, user.UserId);

        // Assert — blacklist key set
        _redisDatabaseMock.Verify(r => r.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("blacklist:")),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task Logout_WithExpiredToken_ShouldNotBlacklistInRedis()
    {
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        _redisDatabaseMock.Setup(r => r.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var act = async () => await _authService.Logout(expiredToken, 1);
        await act.Should().NotThrowAsync();
        
        _redisDatabaseMock.Verify(r => r.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString() == "refresh:1"), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task Logout_ShouldDeleteRefreshTokenFromRedis()
    {
        // Arrange
        var user = new User
        {
            UserId           = 1,
            Email            = "test@example.com",
            PasswordHash     = _hasher.HashPassword(new User(), "Test@1234"),
            IsActive         = true,
            Role             = "USER",
            SubscriptionPlan = "FREE"
        };

        _userRepoMock.Setup(r => r.GetByEmail(user.Email)).ReturnsAsync(user);

        _redisDatabaseMock.Setup(r => r.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _redisDatabaseMock.Setup(r => r.KeyDeleteAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var (accessToken, _) = await _authService.Login(user.Email, "Test@1234");

        // Act
        await _authService.Logout(accessToken, user.UserId);

        // Assert — refresh key deleted
        _redisDatabaseMock.Verify(r => r.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString() == $"refresh:{user.UserId}"),
            It.IsAny<CommandFlags>()),
            Times.Once);
    }

    // ── RefreshToken Tests ────────────────────────────────────────────────────

    [Test]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var userId       = 1;
        var refreshToken = "valid-refresh-token";

        var user = new User
        {
            UserId           = userId,
            Email            = "test@example.com",
            IsActive         = true,
            Role             = "USER",
            SubscriptionPlan = "FREE"
        };

        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.Is<RedisKey>(k => k.ToString() == $"refresh:{userId}"),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(refreshToken));

        _userRepoMock.Setup(r => r.GetById(userId)).ReturnsAsync(user);

        _redisDatabaseMock.Setup(r => r.StringSetAsync(
            It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(),
            It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var (newAccess, newRefresh) = await _authService.RefreshToken(refreshToken, userId);

        // Assert
        newAccess.Should().NotBeNullOrEmpty();
        newRefresh.Should().NotBeNullOrEmpty();
        newAccess.Split('.').Should().HaveCount(3); // valid JWT
        newRefresh.Should().NotBe(refreshToken);   // rotated
    }

    [Test]
    public async Task RefreshToken_WithInvalidToken_ShouldThrowUnauthorized()
    {
        // Arrange — Redis returns a different token (mismatch)
        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("stored-token"));

        // Act
        var act = async () => await _authService.RefreshToken("wrong-token", 1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token");
    }

    [Test]
    public async Task RefreshToken_WithExpiredToken_ShouldThrowUnauthorized()
    {
        // Arrange — Redis returns null (TTL expired)
        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var act = async () => await _authService.RefreshToken("expired-token", 1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token");
    }

    [Test]
    public async Task RefreshToken_WithNonExistentUser_ShouldThrowUnauthorized()
    {
        // Arrange — token matches but user deleted
        var refreshToken = "valid-token";

        _redisDatabaseMock.Setup(r => r.StringGetAsync(
            It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(refreshToken));

        _userRepoMock.Setup(r => r.GetById(99))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _authService.RefreshToken(refreshToken, 99);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User not found");
    }

    // ── GetUserById Tests ─────────────────────────────────────────────────────

    [Test]
    public async Task GetUserById_WithValidId_ShouldReturnUser()
    {
        var user = new User { UserId = 1, Email = "test@example.com", FullName = "Test User" };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);

        var result = await _authService.GetUserById(1);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.Email.Should().Be("test@example.com");
    }

    [Test]
    public async Task GetUserById_WithNonExistentId_ShouldReturnNull()
    {
        // Note: GetUserById returns User? — it does NOT throw
        _userRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((User?)null);

        var result = await _authService.GetUserById(99);

        result.Should().BeNull();
    }

    // ── UpdateProfile Tests ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateProfile_WithValidData_ShouldSaveAndReturnUser()
    {
        // Note: UpdateProfile calls _repo.Update directly — no extra checks in service
        var user = new User
        {
            UserId   = 1,
            FullName = "Updated Name",
            Email    = "updated@example.com",
            Phone    = "9876543210"
        };

        _userRepoMock.Setup(r => r.Update(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        var result = await _authService.UpdateProfile(user);

        result.FullName.Should().Be("Updated Name");
        result.Email.Should().Be("updated@example.com");
        result.Phone.Should().Be("9876543210");
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
    }

    // ── ChangePassword Tests ──────────────────────────────────────────────────

    [Test]
    public async Task ChangePassword_WithCorrectCurrentPassword_ShouldUpdateHash()
    {
        var user = new User
        {
            UserId       = 1,
            Provider     = "LOCAL",
            PasswordHash = _hasher.HashPassword(new User(), "OldPassword")
        };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _authService.ChangePassword(1, "OldPassword", "NewPassword123");

        _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
            u.PasswordHash != _hasher.HashPassword(new User(), "OldPassword")
        )), Times.Once);
    }

    [Test]
    public async Task ChangePassword_WithWrongCurrentPassword_ShouldThrowUnauthorized()
    {
        var user = new User
        {
            UserId       = 1,
            Provider     = "LOCAL",
            PasswordHash = _hasher.HashPassword(new User(), "CorrectPassword")
        };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);

        var act = async () => await _authService.ChangePassword(1, "WrongPassword", "NewPass");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Current password is incorrect");
    }

    [Test]
    public async Task ChangePassword_ForOAuthAccount_ShouldThrowInvalidOperation()
    {
        var user = new User { UserId = 1, Provider = "GOOGLE" };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);

        var act = async () => await _authService.ChangePassword(1, "any", "newpass");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot change password for OAuth accounts");
    }

    [Test]
    public async Task ChangePassword_WithNonExistentUser_ShouldThrowKeyNotFound()
    {
        _userRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((User?)null);

        var act = async () => await _authService.ChangePassword(99, "any", "newpass");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }

    // ── UpdateSubscription Tests ──────────────────────────────────────────────

    [Test]
    public async Task UpdateSubscription_ShouldUpdatePlanToUpperCase()
    {
        var user = new User { UserId = 1, SubscriptionPlan = "FREE" };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _authService.UpdateSubscription(1, "premium");

        _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
            u.SubscriptionPlan == "PREMIUM"
        )), Times.Once);
    }

    [Test]
    public async Task UpdateSubscription_WithNonExistentUser_ShouldThrowKeyNotFound()
    {
        _userRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((User?)null);

        var act = async () => await _authService.UpdateSubscription(99, "PREMIUM");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }

    // ── DeactivateAccount Tests ───────────────────────────────────────────────

    [Test]
    public async Task DeactivateAccount_ShouldSetIsActiveToFalse()
    {
        var user = new User { UserId = 1, IsActive = true };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _authService.DeactivateAccount(1);

        _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
            u.IsActive == false
        )), Times.Once);
    }

    [Test]
    public async Task DeactivateAccount_AlreadyDeactivated_ShouldStillSucceed()
    {
        // Idempotency — calling twice should still work
        var user = new User { UserId = 1, IsActive = false };

        _userRepoMock.Setup(r => r.GetById(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).ReturnsAsync((User u) => u);

        await _authService.DeactivateAccount(1);

        _userRepoMock.Verify(r => r.Update(It.Is<User>(u =>
            u.IsActive == false
        )), Times.Once);
    }

    [Test]
    public async Task DeactivateAccount_WithNonExistentUser_ShouldThrowKeyNotFound()
    {
        _userRepoMock.Setup(r => r.GetById(99)).ReturnsAsync((User?)null);

        var act = async () => await _authService.DeactivateAccount(99);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("User not found");
    }
}