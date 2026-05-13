using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Auth.API.DTOs;
using ResumeAI.Auth.API.Models;
using ResumeAI.Auth.API.Services;

namespace ResumeAI.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = dto.Password, //AuthService will hash this
            Phone = dto.Phone
        };

        var created = await _authService.Register(user);
        return StatusCode(201, new { created.UserId, created.Email, created.FullName });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var (accessToken, refreshToken) = await _authService.Login(dto.Email, dto.Password);

        var user = await _authService.GetUserById(GetUserIdFromToken(accessToken));

        return Ok(new AuthResponseDto
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            UserId       = user!.UserId,
            Plan         = user.SubscriptionPlan
        });
    }

    // POST /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token  = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var userId = GetCurrentUserId();
        await _authService.Logout(token, userId);
        return NoContent();
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var (access, refresh) = await _authService.RefreshToken(dto.RefreshToken, dto.UserId);
        return Ok(new { accessToken = access, refreshToken = refresh });
    }

    // GET /api/auth/profile
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _authService.GetUserById(GetCurrentUserId());
        if (user is null) return NotFound();

        return Ok(new
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            user.Role,
            user.SubscriptionPlan,
            user.IsActive
        });
    }

    // PUT /api/auth/profile
    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _authService.GetUserById(GetCurrentUserId());
        if (user is null) return NotFound();

        if (dto.FullName is not null) user.FullName = dto.FullName;
        if (dto.Email    is not null) user.Email    = dto.Email;
        if (dto.Phone    is not null) user.Phone    = dto.Phone;

        await _authService.UpdateProfile(user);
        return Ok(new { user.UserId, user.FullName, user.Email, user.Phone });
    }

    // PUT /api/auth/password
    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _authService.ChangePassword(
            GetCurrentUserId(), dto.CurrentPassword, dto.NewPassword);
        return NoContent();
    }

    // PUT /api/auth/subscription
    [Authorize]
    [HttpPut("subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdateSubscriptionDto dto)
    {
        await _authService.UpdateSubscription(GetCurrentUserId(), dto.Plan);
        return NoContent();
    }

    // DELETE /api/auth/deactivate
    [Authorize]
    [HttpDelete("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        await _authService.DeactivateAccount(GetCurrentUserId());
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static int GetUserIdFromToken(string jwt)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token   = handler.ReadJwtToken(jwt);
        return int.Parse(token.Claims
            .First(c => c.Type == ClaimTypes.NameIdentifier).Value);
    }
}