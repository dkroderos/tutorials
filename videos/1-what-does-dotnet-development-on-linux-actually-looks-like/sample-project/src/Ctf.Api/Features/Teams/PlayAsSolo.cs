using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;
using FluentValidation;

namespace Ctf.Api.Features.Teams;

public static class PlayAsSolo
{
    public sealed record PlayAsSoloRequest(Guid RoomId, string Name);

    public sealed record Command(Guid UserId, Guid RoomId, string Name);

    public sealed record Response(Guid Id);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IRoomRepository roomRepository,
        ITeamRepository teamRepository,
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

            if (role != RoomRole.Player)
                return Result.Failure<Response>(TeamMemberErrors.CandidateMustBeAPlayer);

            var allowsUserCreatedTeams = await roomRepository.AllowsPlayerCreatedTeamsAsync(
                request.RoomId
            );

            if (allowsUserCreatedTeams is null)
                return Result.Failure<Response>(RoomErrors.NotFound);
            if (!allowsUserCreatedTeams.Value)
                return Result.Failure<Response>(RoomErrors.PlayerCreatedTeamsNotAllowed);

            var nameInRoomExists = await teamRepository.NameInRoomExistsAsync(
                request.RoomId,
                request.Name
            );

            if (nameInRoomExists)
                return Result.Failure<Response>(TeamErrors.NameAlreadyExists);

            var dto = new CreateTeamDto { RoomId = request.RoomId, Name = request.Name };
            var id = await teamRepository.CreateAsync(dto, request.UserId);

            return new Response(id);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "teams/solo",
                    async (PlayAsSoloRequest request, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(
                            claims.GetLoggedInUserId(),
                            request.RoomId,
                            request.Name
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Teams));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage(TeamConstants.NameRequiredMessage)
                .MaximumLength(TeamConstants.NameMaxLength)
                .WithMessage(TeamConstants.NameMaxLengthExceededMessage);
        }
    }
}
