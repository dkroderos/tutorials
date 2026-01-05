using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.Features.TeamMembers;

public static class RemoveTeamMember
{
    public sealed record RemoveTeamMemberRequest(Guid TargetUserId);

    public sealed record Command(Guid TeamId, Guid RemoverId, Guid TargetUserId);

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

            var adderRole = await roomMemberRepository.GetRoleAsync(
                roomId.Value,
                request.RemoverId
            );
            if (adderRole is null)
                return Result.Failure(TeamErrors.NotFound);

            if (adderRole < RoomRole.Admin)
                return Result.Failure(TeamErrors.LoggedUserNotAnAdmin);

            var deleted = await teamMemberRepository.RemoveAsync(
                request.TeamId,
                request.TargetUserId
            );
            if (!deleted)
                return Result.Failure(TeamMemberErrors.UserNotInTheTeam);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "teams/{teamid:guid}/members/{targetid:guid}",
                    async (Guid teamId, Guid targetId, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(teamId, claims.GetLoggedInUserId(), targetId);

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(TeamMembers));
        }
    }
}
