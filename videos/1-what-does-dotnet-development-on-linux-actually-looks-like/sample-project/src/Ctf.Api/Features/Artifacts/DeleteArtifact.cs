using System.Security.Claims;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.Artifacts;

public static class DeleteArtifact
{
    public sealed record Command(Guid RemoverId, Guid ChallengeId, string FileName);

    public sealed class Handler(
        IChallengeRepository challengeRepository,
        IRoomMemberRepository roomMemberRepository,
        IArtifactRepository artifactRepository
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var roomId = await challengeRepository.GetRoomIdAsync(request.ChallengeId);
            if (roomId is null)
                return Result.Failure(ArtifactErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.RemoverId);
            if (role is null)
                return Result.Failure(ArtifactErrors.NotFound);

            if (role < RoomRole.Editor)
                return Result.Failure(ChallengeErrors.LoggedUserNotAnEditor);

            var deleted = await artifactRepository.DeleteAsync(
                request.ChallengeId,
                request.FileName
            );

            if (!deleted)
                return Result.Failure<Stream>(ArtifactErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "challenges/{challengeid:guid}/artifacts/{filename}",
                    async (
                        Guid challengeId,
                        string fileName,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var query = new Command(claims.GetLoggedInUserId(), challengeId, fileName);

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Artifacts));
        }
    }
}
