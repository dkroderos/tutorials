using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.RoomMembers;

public static class LeaveRoom
{
    public sealed record Command(Guid RoomId, Guid UserId);

    public sealed class Handler(IRoomMemberRepository roomMemberRepository) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var role = await roomMemberRepository.GetRoleAsync(request.RoomId, request.UserId);

            if (role is null)
                return Result.Failure(RoomErrors.NotFound);

            if (role == RoomRole.Owner)
                return Result.Failure(RoomMemberErrors.OwnerCannotLeave);

            var deleted = await roomMemberRepository.DeleteAsync(request.RoomId, request.UserId);

            if (!deleted)
                return Result.Failure(RoomErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "rooms/{id:guid}/leave",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(id, claims.GetLoggedInUserId());

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(RoomMembers));
        }
    }
}
