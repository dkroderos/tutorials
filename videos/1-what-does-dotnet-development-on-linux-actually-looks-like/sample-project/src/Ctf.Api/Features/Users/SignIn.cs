using Ctf.Api.Helpers.Security;
using Ctf.Api.Options;
using Ctf.Api.Repositories.Users;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Ctf.Api.Features.Users;

public static class SignIn
{
    public sealed record SignInRequest(string Email, string Password);

    public sealed record Command(string Email, string Password, string IpAddress);

    public sealed class Handler(
        IUserRepository userRepository,
        IAuthHelper authHelper,
        IPasswordHasher passwordHasher,
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

            var userDto = await userRepository.GetByEmailAsync(request.Email);
            if (userDto is null || userDto.PasswordHash is null)
                return Result.Failure<AuthResponseWithRefreshToken>(
                    UserErrors.IncorrectEmailOrPassword
                );

            var correctPassword = passwordHasher.Verify(request.Password, userDto.PasswordHash);
            if (!correctPassword)
                return Result.Failure<AuthResponseWithRefreshToken>(
                    UserErrors.IncorrectEmailOrPassword
                );

            if (!userDto.IsVerified)
                return Result.Failure<AuthResponseWithRefreshToken>(UserErrors.EmailNotVerified);

            return await authHelper.CreateAuthResponseWithRefreshTokenAsync(
                userDto.Id,
                userDto.Username
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "signin",
                    async (
                        SignInRequest request,
                        Handler handler,
                        IClientIpResolver clientIpResolver,
                        IOptionsMonitor<JwtOptions> jwtOptions,
                        HttpContext httpContext
                    ) =>
                    {
                        var ipAddress = clientIpResolver.GetClientIp(httpContext);
                        var command = new Command(request.Email, request.Password, ipAddress);

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

            RuleFor(x => x.Password)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(PasswordConstants.RequiredMessage)
                .MinimumLength(PasswordConstants.MinLength)
                .WithMessage(PasswordConstants.MinLengthNotMetMessage)
                .MaximumLength(PasswordConstants.MaxLength)
                .WithMessage(PasswordConstants.MaxLengthExceededMessage)
                .Matches(PasswordConstants.HasUppercase)
                .WithMessage(PasswordConstants.HasUppercaseMessage)
                .Matches(PasswordConstants.HasLowercase)
                .WithMessage(PasswordConstants.HasLowercaseMessage);
        }
    }
}
