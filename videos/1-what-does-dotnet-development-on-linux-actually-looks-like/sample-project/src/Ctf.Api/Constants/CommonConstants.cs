namespace Ctf.Api.Constants;

public static class CommonConstants
{
    public const string RefreshTokenCookieKey = "refresh-token";
    public const int DefaultPageSize = 10;
    public const string Google = "Google";
    public const string GoogleClientId = "Authentication:Google:ClientId";
    public const string GoogleClientSecret = "Authentication:Google:ClientSecret";
    public const string JwtAudience = "JwtOptions:Audience";
    public const string JwtAccessTokenLifetime = "JwtOptions:AccessTokenLifetime";
    public const string JwtIssuer = "JwtOptions:Issuer";
    public const string JwtSecret = "JwtOptions:Secret";
    public const int MaxPageSize = 20;
    public const string MissingIpAddress = "Missing or invalid IP address.";
}
