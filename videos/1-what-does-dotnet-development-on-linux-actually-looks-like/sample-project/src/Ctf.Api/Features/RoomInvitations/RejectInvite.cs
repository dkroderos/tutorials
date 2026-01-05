using System.Security.Claims;
using Ctf.Api.Repositories.RoomIntivations;

namespace Ctf.Api.Features.RoomInvitations;

public static class RejectInvite
{
    public sealed record Command(Guid RoomId, Guid InviteeId);

    public sealed class Handler(IRoomInvitationRepository roomInvitationRepository) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var deleted = await roomInvitationRepository.DeleteAsync(
                request.RoomId,
                request.InviteeId
            );

            if (!deleted)
                return Result.Failure(RoomInvitationErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "rooms/{id:guid}/reject",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(id, claims.GetLoggedInUserId());

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(RoomInvitations));
        }
    }
}
