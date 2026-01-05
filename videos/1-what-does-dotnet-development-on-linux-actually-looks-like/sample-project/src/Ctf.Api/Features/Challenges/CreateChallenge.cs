using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using FluentValidation;

namespace Ctf.Api.Features.Challenges;

public static class CreateChallenge
{
    public sealed record CreateChallengeRequest(
        Guid RoomId,
        string Name,
        string Description,
        int MaxAttempts,
        string[] Tags,
        string[] Flags
    );

    public sealed record Command(
        Guid RoomId,
        Guid UserId,
        string Name,
        string Description,
        int MaxAttempts,
        string[] Tags,
        string[] Flags
    );

    public sealed record Response(Guid Id);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IChallengeRepository challengeRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<Response>(Error.Validation(validationResult.ToString()));

            var role = await roomMemberRepository.GetRoleAsync(request.RoomId, request.UserId);

            if (role is null)
                return Result.Failure<Response>(RoomErrors.NotFound);

            if (role < RoomRole.Editor)
                return Result.Failure<Response>(ChallengeErrors.LoggedUserNotAnEditor);

            var nameInRoomExists = await challengeRepository.NameInRoomExistsAsync(
                request.Name,
                request.RoomId
            );

            if (nameInRoomExists)
                return Result.Failure<Response>(ChallengeErrors.NameAlreadyExists);

            var dto = new CreateChallengeDto
            {
                RoomId = request.RoomId,
                CreatorId = request.UserId,
                Name = request.Name,
                Description = request.Description,
                MaxAttempts = request.MaxAttempts,
                Flags = request.Flags,
                Tags = request.Tags,
            };
            var id = await challengeRepository.CreateAsync(dto);

            var response = new Response(id);
            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "challenges",
                    async (
                        CreateChallengeRequest request,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(
                            request.RoomId,
                            claims.GetLoggedInUserId(),
                            request.Name,
                            request.Description,
                            request.MaxAttempts,
                            request.Flags,
                            request.Tags
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Challenges));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage(ChallengeConstants.NameRequiredMessage)
                .MaximumLength(ChallengeConstants.NameMaxLength)
                .WithMessage(ChallengeConstants.NameMaxLengthExceededMessage);

            RuleFor(c => c.Description)
                .NotEmpty()
                .WithMessage(ChallengeConstants.DescriptionRequiredMessage)
                .MaximumLength(ChallengeConstants.DescriptionMaxLength)
                .WithMessage(ChallengeConstants.DescriptionMaxLengthExceededMessage);

            RuleFor(c => c.MaxAttempts)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ChallengeConstants.MaxAttemptsLessThanZeroMessage);

            RuleFor(c => c.Flags)
                .NotEmpty()
                .WithMessage(FlagConstants.RequiredMessage)
                .Must(flags => flags.All(flag => !string.IsNullOrWhiteSpace(flag)))
                .WithMessage(FlagConstants.MustBeNonEmptyMessage)
                .Must(flags => flags.All(flag => flag.Length <= FlagConstants.MaxLength))
                .WithMessage(FlagConstants.MaxLengthExceededMessage);

            RuleFor(c => c.Tags)
                .Must(tags => tags.All(flag => !string.IsNullOrWhiteSpace(flag)))
                .WithMessage(TagConstants.MustBeNonEmptyMessage)
                .Must(tags => tags.All(tag => tag.Length <= TagConstants.MaxLength))
                .WithMessage(TagConstants.MaxLengthExceededMessage);
        }
    }
}
