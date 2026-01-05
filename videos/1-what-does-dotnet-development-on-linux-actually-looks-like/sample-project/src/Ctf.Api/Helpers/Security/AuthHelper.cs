using Ctf.Api.Repositories.RefreshTokens;

namespace Ctf.Api.Helpers.Security;

public sealed class AuthHelper(
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokenRepository
) : IAuthHelper
{
    public async Task<AuthResponseWithRefreshToken> CreateAuthResponseWithRefreshTokenAsync(
        Guid userId,
        string username
    )
    {
        var refreshToken = tokenProvider.CreateRefreshToken();
        var addRefreshTokenDto = new AddRefreshTokenDto { Token = refreshToken, UserId = userId };

        await refreshTokenRepository.AddAsync(addRefreshTokenDto);

        var accessToken = tokenProvider.CreateAccessToken(userId, username);
        var response = new AuthResponseWithRefreshToken(
            userId,
            username,
            accessToken,
            refreshToken
        );

        return response;
    }
}

public interface IAuthHelper
{
    Task<AuthResponseWithRefreshToken> CreateAuthResponseWithRefreshTokenAsync(
        Guid userId,
        string username
    );
}

public sealed record AuthResponseWithRefreshToken(
    Guid Id,
    string Username,
    string AccessToken,
    string RefreshToken
);

public sealed record AuthResponseWithoutRefreshToken(Guid Id, string Username, string AccessToken);
