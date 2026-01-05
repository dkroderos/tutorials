using System.Security.Cryptography;
using System.Text;
using Ctf.Api.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Ctf.Api.Repositories.RefreshTokens;

public sealed class RefreshTokenRepository(
    IConnectionMultiplexer mux,
    IOptionsMonitor<JwtOptions> jwtOptions
) : IRefreshTokenRepository
{
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task AddAsync(AddRefreshTokenDto dto)
    {
        var tokenKey = GetTokenKey(dto.Token);
        var userTokensKey = GetUserTokensKey(dto.UserId);
        var refreshTokenLifetime = jwtOptions.CurrentValue.RefreshTokenLifetime;

        await _db.StringSetAsync(tokenKey, dto.UserId.ToString(), refreshTokenLifetime);
        await _db.SetAddAsync(userTokensKey, tokenKey);
        await _db.KeyExpireAsync(userTokensKey, refreshTokenLifetime);
    }

    public async Task<Guid?> GetUserIdByTokenAsync(string token)
    {
        var tokenKey = GetTokenKey(token);
        var value = await _db.StringGetAsync(tokenKey);
        if (!value.HasValue)
            return null;

        var valueString = (string?)value;

        return Guid.TryParse(valueString, out var userId) ? userId : null;
    }

    public async Task<bool> IsValidAsync(string token, Guid userId)
    {
        var tokenKey = GetTokenKey(token);
        var value = await _db.StringGetAsync(tokenKey);
        return value.HasValue && value == userId.ToString();
    }

    public async Task RemoveAsync(string token, Guid userId)
    {
        var tokenKey = GetTokenKey(token);
        var userTokensKey = GetUserTokensKey(userId);

        await _db.KeyDeleteAsync(tokenKey);
        await _db.SetRemoveAsync(userTokensKey, tokenKey);
    }

    public async Task RemoveAllAsync(Guid userId)
    {
        var userTokensKey = GetUserTokensKey(userId);
        var tokens = await _db.SetMembersAsync(userTokensKey);

        foreach (var token in tokens)
        {
            if (!token.HasValue)
                continue;

            string tokenKey = token!;
            await _db.KeyDeleteAsync(tokenKey);
        }

        await _db.KeyDeleteAsync(userTokensKey);
    }

    private static string GetTokenKey(string token) =>
        $"refresh-token:{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)))}";

    private static string GetUserTokensKey(Guid userId) => $"user-refresh-token:{userId}";
}
