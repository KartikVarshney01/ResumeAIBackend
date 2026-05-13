using System.ComponentModel.DataAnnotations;

namespace ResumeAI.Auth.API.DTOs;

public class RegisterRequestDto
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpireIn { get; set; } = 86400;
    public int UserId { get; set; }
    public string Plan { get; set; } = "FREE";
}

public class UpdateProfileDto
{
    [MaxLength(120)]
    public string? FullName { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateSubscriptionDto
{
    [Required]
    public string Plan { get; set; } = string.Empty; // FREE | PREMIUM
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }
}



