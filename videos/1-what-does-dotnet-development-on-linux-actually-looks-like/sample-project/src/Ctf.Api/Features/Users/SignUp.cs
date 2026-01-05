using Ctf.Api.Helpers.Security;
using Ctf.Api.Repositories.Users;
using FluentValidation;

namespace Ctf.Api.Features.Users;

public static class SignUp
{
    public sealed record SignUpRequest(string Username, string Email, string Password);

    public sealed record Command(string Username, string Email, string Password, string IpAddress);

    public sealed record Response(Guid Id);

    public sealed class Handler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<Response>(Error.Validation(validationResult.ToString()));

            var usernameExists = await userRepository.UsernameExistsAsync(request.Username);
            if (usernameExists)
                return Result.Failure<Response>(UserErrors.UsernameTaken);

            var emailExists = await userRepository.EmailExistsAsync(request.Email);
            if (emailExists)
                return Result.Failure<Response>(UserErrors.EmailTaken);

            var dto = new CreateUserDto
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHasher.Hash(request.Password),
                RegistrationIp = request.IpAddress,
                IsVerified = true,
            };

            var id = await userRepository.CreateAsync(dto);

            var response = new Response(id);
            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "signup",
                    async (
                        SignUpRequest request,
                        Handler handler,
                        IClientIpResolver clientIpResolver,
                        HttpContext httpContext
                    ) =>
                    {
                        var ipAddress = clientIpResolver.GetClientIp(httpContext);
                        var command = new Command(
                            request.Username,
                            request.Email,
                            request.Password,
                            ipAddress
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .WithTags(nameof(Users));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Username)
                .NotEmpty()
                .WithMessage(UserConstants.UsernameRequiredMessage)
                .MaximumLength(UserConstants.UsernameMaxLength)
                .WithMessage(UserConstants.UsernameMaxLengthExceededMessage);

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
