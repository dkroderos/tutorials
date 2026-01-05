using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.Features.Teams;

public static class DeleteTeam
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(
        ITeamRepository teamRepository,
        IRoomMemberRepository roomMemberRepository
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var roomId = await teamRepository.GetRoomIdAsync(request.Id);
            if (roomId is null)
                return Result.Failure(TeamErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (role is null)
                return Result.Failure(TeamErrors.NotFound);

            if (role < RoomRole.Admin)
                return Result.Failure(TeamErrors.LoggedUserNotAnAdmin);

            var deleted = await teamRepository.DeleteAsync(request.Id);
            if (!deleted)
                return Result.Failure(TeamErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "teams/{id:guid}",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(id, claims.GetLoggedInUserId());

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Teams));
        }
    }
}
