using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ctf.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ctf.Api.Helpers.Security;

public sealed class TokenProvider(IOptionsMonitor<JwtOptions> jwtOptions) : ITokenProvider
{
    private const int RefreshTokenByteSize = 32;

    public string CreateAccessToken(Guid userId, string username)
    {
        var options = jwtOptions.CurrentValue;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = [new(JwtRegisteredClaimNames.Sub, userId.ToString())];
        if (!string.IsNullOrEmpty(username))
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, username));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow + options.AccessTokenLifetime,
            SigningCredentials = credentials,
            Issuer = options.Issuer,
            Audience = options.Audience,
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return token;
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteSize));
}
