using System.ComponentModel.DataAnnotations;

namespace Ctf.Api.Options;

public sealed class JwtOptions
{
    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Required]
    public required string Secret { get; init; }

    [Required]
    public required TimeSpan AccessTokenLifetime { get; init; }

    [Required]
    public required TimeSpan RefreshTokenLifetime { get; init; }

    [Required]
    public required bool SecureRefreshTokenCookie { get; init; }
}
