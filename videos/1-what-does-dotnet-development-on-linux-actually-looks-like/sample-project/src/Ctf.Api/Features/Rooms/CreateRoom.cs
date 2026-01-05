using System.Security.Claims;
using Ctf.Api.Repositories.Rooms;
using FluentValidation;

namespace Ctf.Api.Features.Rooms;

public static class CreateRoom
{
    public sealed record CreateRoomRequest(
        string Name,
        string Description,
        bool AreChallengesHidden,
        bool IsSubmissionsForceDisabled,
        bool AllowPlayerCreatedTeams,
        bool AllowPlayersToViewOtherTeamSolves,
        DateTime SubmissionStart,
        DateTime SubmissionEnd
    );

    public sealed record Command(
        Guid UserId,
        string Name,
        string Description,
        bool AreChallengesHidden,
        bool IsSubmissionsForceDisabled,
        bool AllowPlayerCreatedTeams,
        bool AllowPlayersToViewOtherTeamSolves,
        DateTime SubmissionStart,
        DateTime SubmissionEnd
    );

    public sealed record Response(Guid Id);

    public sealed class Handler(IRoomRepository roomRepository, IValidator<Command> validator)
        : IFeature
    {
        public async Task<Result<Response>> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<Response>(Error.Validation(validationResult.ToString()));

            var exists = await roomRepository.ExistsByNameAsync(request.UserId, request.Name);
            if (exists)
                return Result.Failure<Response>(RoomErrors.NameAlreadyExists);

            var dto = new CreateRoomDto
            {
                CreatorId = request.UserId,
                Name = request.Name,
                Description = request.Description,
                AreChallengesHidden = request.AreChallengesHidden,
                IsSubmissionsForceDisabled = request.IsSubmissionsForceDisabled,
                AllowPlayerCreatedTeams = request.AllowPlayerCreatedTeams,
                AllowPlayersToViewOtherTeamSolves = request.AllowPlayersToViewOtherTeamSolves,
                SubmissionStart = request.SubmissionStart,
                SubmissionEnd = request.SubmissionEnd,
            };
            var id = await roomRepository.CreateAsync(dto);

            var response = new Response(id);
            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "rooms",
                    async (CreateRoomRequest request, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(
                            claims.GetLoggedInUserId(),
                            request.Name,
                            request.Description,
                            request.AreChallengesHidden,
                            request.IsSubmissionsForceDisabled,
                            request.AllowPlayerCreatedTeams,
                            request.AllowPlayersToViewOtherTeamSolves,
                            request.SubmissionStart,
                            request.SubmissionEnd
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Rooms));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage(RoomConstants.NameRequiredMessage)
                .MaximumLength(RoomConstants.NameMaxLength)
                .WithMessage(RoomConstants.NameMaxLengthExceededMessage);

            RuleFor(c => c.Description)
                .MaximumLength(RoomConstants.DescriptionMaxLength)
                .WithMessage(RoomConstants.DescriptionMaxLengthExceededMessage);
        }
    }
}
