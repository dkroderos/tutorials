using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.Challenges;

public static class DeleteChallenge
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IChallengeRepository challengeRepository
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var roomId = await challengeRepository.GetRoomIdAsync(request.Id);
            if (roomId is null)
                return Result.Failure(ChallengeErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (role is null)
                return Result.Failure(ChallengeErrors.NotFound);

            if (role < RoomRole.Editor)
                return Result.Failure(ChallengeErrors.LoggedUserNotAnEditor);

            var deleted = await challengeRepository.DeleteAsync(request.Id);

            if (!deleted)
                return Result.Failure(ChallengeErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "challenges/{id:guid}",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(id, claims.GetLoggedInUserId());

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Challenges));
        }
    }
}
