using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.Features.TeamMembers;

public static class AddTeamMember
{
    public sealed record AddTeamMemberRequest(Guid CandidateId);

    public sealed record Command(Guid TeamId, Guid AdderId, Guid CandidateId);

    public sealed class Handler(
        ITeamRepository teamRepository,
        IRoomMemberRepository roomMemberRepository,
        ITeamMemberRepository teamMemberRepository
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var roomId = await teamRepository.GetRoomIdAsync(request.TeamId);
            if (roomId is null)
                return Result.Failure(TeamErrors.NotFound);

            var adderRole = await roomMemberRepository.GetRoleAsync(roomId.Value, request.AdderId);
            if (adderRole is null)
                return Result.Failure(TeamErrors.NotFound);

            if (adderRole < RoomRole.Admin)
                return Result.Failure(TeamErrors.LoggedUserNotAnAdmin);

            var candidateRole = await roomMemberRepository.GetRoleAsync(
                roomId.Value,
                request.CandidateId
            );
            if (candidateRole is null)
                return Result.Failure(RoomMemberErrors.NotAMember);

            if (candidateRole != RoomRole.Player)
                return Result.Failure(TeamMemberErrors.CandidateMustBeAPlayer);

            var currentUserTeam = await teamMemberRepository.GetUserTeamAsync(
                roomId.Value,
                request.CandidateId
            );
            if (currentUserTeam is not null)
                return Result.Failure(TeamMemberErrors.AlreadyHasTeam);

            var dto = new AddTeamMemberDto
            {
                TeamId = request.TeamId,
                UserId = request.CandidateId,
                RoomId = roomId.Value,
            };

            await teamMemberRepository.AddAsync(dto);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "teams/{id:guid}/members",
                    async (
                        Guid id,
                        AddTeamMemberRequest request,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(
                            id,
                            claims.GetLoggedInUserId(),
                            request.CandidateId
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(TeamMembers));
        }
    }
}
