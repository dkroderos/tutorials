using Ctf.Api.Helpers.Security;
using Ctf.Api.Repositories.RefreshTokens;
using Ctf.Api.Repositories.Users;

namespace Ctf.Api.Features.Users;

public static class Refresh
{
    public sealed record Command(string? RefreshToken);

    public sealed class Handler(
        ITokenProvider tokenProvider,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository
    ) : IFeature
    {
        public async Task<Result<AuthResponseWithoutRefreshToken>> Handle(Command request)
        {
            if (request.RefreshToken is null)
                return Result.Failure<AuthResponseWithoutRefreshToken>(UserErrors.InvalidAccess);

            var userId = await refreshTokenRepository.GetUserIdByTokenAsync(request.RefreshToken);
            if (!userId.HasValue)
                return Result.Failure<AuthResponseWithoutRefreshToken>(UserErrors.InvalidAccess);

            var username = await userRepository.GetUsernameByIdAsync(userId.Value);
            if (username is null)
                return Result.Failure<AuthResponseWithoutRefreshToken>(UserErrors.InvalidAccess);

            var newAccessToken = tokenProvider.CreateAccessToken(userId.Value, username);

            var response = new AuthResponseWithoutRefreshToken(
                userId.Value,
                username,
                newAccessToken
            );

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "refresh",
                    async (Handler handler, HttpContext httpContext) =>
                    {
                        var refreshToken = httpContext.Request.Cookies[
                            CommonConstants.RefreshTokenCookieKey
                        ];

                        var command = new Command(refreshToken);
                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .WithTags(nameof(Users));
        }
    }
}
