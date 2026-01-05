using System.Security.Claims;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.RoomMembers;

namespace Ctf.Api.Features.RoomInvitations;

public static class AcceptInvite
{
    public sealed record Command(Guid InviteeId, Guid RoomId);

    public sealed class Handler(
        IRoomInvitationRepository roomInvitationRepository,
        IRoomMemberRepository roomMemberRepository
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var alreadyMember = await roomMemberRepository.ExistsAsync(
                request.RoomId,
                request.InviteeId
            );

            if (alreadyMember)
            {
                await roomInvitationRepository.DeleteAsync(request.RoomId, request.InviteeId);
                return Result.Failure(RoomMemberErrors.LoggedUserAlreadyMember);
            }

            var accepted = await roomInvitationRepository.AcceptAsync(
                request.RoomId,
                request.InviteeId
            );

            if (!accepted)
                return Result.Failure(RoomInvitationErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut(
                    "rooms/{id:guid}/accept",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(claims.GetLoggedInUserId(), id);

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(RoomInvitations));
        }
    }
}
