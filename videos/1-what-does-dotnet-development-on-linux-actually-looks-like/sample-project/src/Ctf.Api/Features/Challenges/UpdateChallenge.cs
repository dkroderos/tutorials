using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using FluentValidation;

namespace Ctf.Api.Features.Challenges;

public static class UpdateChallenge
{
    public sealed record UpdateChallengeRequest(
        Guid Id,
        string Name,
        string Description,
        int MaxAttempts,
        string[] Flags,
        string[] Tags
    );

    public sealed record Command(
        Guid Id,
        Guid UserId,
        string Name,
        string Description,
        int MaxAttempts,
        string[] Flags,
        string[] Tags
    );

    public sealed record Response(Guid UpdatedId);

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

            var roomId = await challengeRepository.GetRoomIdAsync(request.Id);
            if (roomId is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (role is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            if (role < RoomRole.Editor)
                return Result.Failure<Response>(ChallengeErrors.LoggedUserNotAnEditor);

            var dto = new UpdateChallengeDto
            {
                Id = request.Id,
                UpdaterId = request.UserId,
                Name = request.Name,
                Description = request.Description,
                MaxAttempts = request.MaxAttempts,
                Flags = request.Flags,
                Tags = request.Tags,
            };
            var updated = await challengeRepository.UpdateAsync(dto);

            if (!updated)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            return new Response(request.Id);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "challenges",
                    async (
                        UpdateChallengeRequest request,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(
                            request.Id,
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
                .Must(flags => flags.Length <= FlagConstants.MaxCount)
                .WithMessage(FlagConstants.MaxCountExceededMessage)
                .Must(flags => flags.All(flag => !string.IsNullOrWhiteSpace(flag)))
                .WithMessage(FlagConstants.MustBeNonEmptyMessage)
                .Must(flags => flags.All(flag => flag.Length <= FlagConstants.MaxLength))
                .WithMessage(FlagConstants.MaxLengthExceededMessage);

            RuleFor(c => c.Tags)
                .Must(tags => tags.Length <= TagConstants.MaxCount)
                .WithMessage(TagConstants.MaxCountExceededMessage)
                .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
                .WithMessage(TagConstants.MustBeNonEmptyMessage)
                .Must(tags => tags.All(tag => tag.Length <= TagConstants.MaxLength))
                .WithMessage(TagConstants.MaxLengthExceededMessage);
        }
    }
}
