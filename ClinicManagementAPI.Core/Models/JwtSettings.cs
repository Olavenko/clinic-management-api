namespace ClinicManagementAPI.Core.Models;

// This class is used to store the JWT settings from appsettings.json
public class JwtSettings
{
    // The JWT secret key — must be at least 32 characters long
    public string Key { get; set; } = string.Empty;
    // The JWT issuer — the entity that issues the token
    public string Issuer { get; set; } = string.Empty;
    // The JWT audience — the entity that the token is intended for
    public string Audience { get; set; } = string.Empty;
    // The JWT expiry time in minutes
    public int ExpiryMinutes { get; set; }
    // The JWT refresh token expiry time in days
    public int RefreshTokenExpiryDays { get; set; }
}