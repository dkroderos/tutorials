using System.Security.Claims;
using Ctf.Api.Helpers.Security;
using Ctf.Api.Options;
using Ctf.Api.Repositories.Users;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Ctf.Api.Features.Users;

public static class SignInWithGoogle
{
    public sealed record Command(string GoogleId, string Email, string Username, string IpAddress);

    public sealed class Handler(
        IUserRepository userRepository,
        IAuthHelper authHelper,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result<AuthResponseWithRefreshToken>> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<AuthResponseWithRefreshToken>(
                    Error.Validation(validationResult.ToString())
                );

            var existingUserId = await userRepository.GetIdByExternalProviderAsync(
                ExternalProvider.Google,
                request.GoogleId
            );

            return existingUserId is null
                ? await HandleNewUserAsync(request)
                : await HandleExistingUserAsync(existingUserId.Value);
        }

        private async Task<Result<AuthResponseWithRefreshToken>> HandleNewUserAsync(Command request)
        {
            var userId = await userRepository.CreateAsync(
                new CreateUserDto
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = null,
                    IsVerified = true,
                    RegistrationIp = request.IpAddress,
                },
                new CreateUserProviderDto
                {
                    Provider = ExternalProvider.Google,
                    ProviderId = request.GoogleId,
                }
            );

            var response = await authHelper.CreateAuthResponseWithRefreshTokenAsync(
                userId,
                request.Username
            );
            return response;
        }

        private async Task<Result<AuthResponseWithRefreshToken>> HandleExistingUserAsync(
            Guid userId
        )
        {
            var user = await userRepository.GetByIdAsync(userId);
            if (user is null)
                return Result.Failure<AuthResponseWithRefreshToken>(UserErrors.NotFound);

            if (!user.IsVerified)
                return Result.Failure<AuthResponseWithRefreshToken>(UserErrors.EmailNotVerified);

            var response = await authHelper.CreateAuthResponseWithRefreshTokenAsync(
                user.Id,
                user.Username
            );
            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "signin/google",
                async httpContext =>
                {
                    await httpContext.ChallengeAsync(
                        CommonConstants.Google,
                        new AuthenticationProperties { RedirectUri = "/signin/google/callback" }
                    );
                }
            );

            app.MapGet(
                    "signin/google/callback",
                    async (
                        Handler handler,
                        IClientIpResolver clientIpResolver,
                        IOptionsMonitor<JwtOptions> jwtOptions,
                        HttpContext httpContext
                    ) =>
                    {
                        var ipAddress = clientIpResolver.GetClientIp(httpContext);
                        var googleAuthFailed = Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            detail: "Google authentication failed."
                        );

                        var authenticateResult = await httpContext.AuthenticateAsync(
                            CommonConstants.Google
                        );
                        if (!authenticateResult.Succeeded)
                            return googleAuthFailed;

                        var claims = authenticateResult.Principal.Claims.ToList();
                        var googleId = claims
                            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                            ?.Value;
                        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                        var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                        if (email is null || googleId is null || username is null)
                            return googleAuthFailed;

                        var command = new Command(googleId, email, username, ipAddress);
                        var result = await handler.Handle(command);
                        return result.ToSignInResult(
                            jwtOptions.CurrentValue,
                            httpContext,
                            result => result.RefreshToken,
                            response => new AuthResponseWithoutRefreshToken(
                                response.Id,
                                response.Username,
                                response.AccessToken
                            )
                        );
                    }
                )
                .WithTags(nameof(Users));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Email)
                .NotEmpty()
                .WithMessage(UserEmailConstants.EmailRequiredMessage)
                .EmailAddress()
                .WithMessage(UserEmailConstants.InvalidEmailFormatMessage);
        }
    }
}
