using System.Security.Claims;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;

namespace Ctf.Api.Features.Artifacts;

public static class GetArtifact
{
    public sealed record Query(Guid UserId, Guid ChallengeId, string FileName);

    public sealed class Handler(
        IChallengeRepository challengeRepository,
        IRoomMemberRepository roomMemberRepository,
        IArtifactRepository artifactRepository
    ) : IFeature
    {
        public async Task<Result<Stream>> Handle(Query request)
        {
            var roomId = await challengeRepository.GetRoomIdAsync(request.ChallengeId);
            if (roomId is null)
                return Result.Failure<Stream>(RoomErrors.NotFound);

            var isMember = await roomMemberRepository.ExistsAsync(roomId.Value, request.UserId);
            if (!isMember)
                return Result.Failure<Stream>(ChallengeErrors.NotFound);

            var stream = await artifactRepository.GetStreamAsync(
                request.ChallengeId,
                request.FileName
            );

            if (stream is null)
                return Result.Failure<Stream>(ArtifactErrors.NotFound);

            return stream;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "challenges/{challengeid:guid}/artifacts/{filename}",
                    async (
                        Guid challengeId,
                        string fileName,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var query = new Query(claims.GetLoggedInUserId(), challengeId, fileName);

                        var result = await handler.Handle(query);

                        return result.IsSuccess
                            ? Results.File(
                                fileStream: result.Value,
                                contentType: "application/octet-stream",
                                fileDownloadName: fileName
                            )
                            : Results.Problem(
                                statusCode: result.Error.Code,
                                detail: result.Error.Detail
                            );
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Artifacts));
        }
    }
}
