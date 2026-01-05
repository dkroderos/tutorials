using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.Challenges;

public static class GetChallenge
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed record Response(
        Guid Id,
        Guid RoomId,
        string RoomName,
        string Name,
        string Description,
        int MaxAttempts,
        Guid CreatorId,
        string CreatorUsername,
        DateTime CreatedAt,
        Guid? UpdaterId,
        string? UpdaterUsername,
        DateTime? UpdatedAt,
        ArtifactResponse[] Artifacts,
        int FlagsCount,
        string[] Tags
    );

    public sealed record ArtifactResponse(
        Guid ChallengeId,
        string FileName,
        long FileSize,
        Guid? UploaderId,
        string ContentType,
        DateTime CreatedAt
    );

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IRoomRepository roomRepository,
        IChallengeRepository challengeRepository
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Query request)
        {
            var dto = await challengeRepository.GetDetailsAsync(request.Id);
            if (dto is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var isMember = await roomMemberRepository.ExistsAsync(dto.RoomId, request.UserId);
            if (!isMember)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var areChallengesHidden = await roomRepository.AreChallengesHiddenAsync(dto.RoomId);
            if (areChallengesHidden is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            if (areChallengesHidden.Value)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var response = new Response(
                dto.Id,
                dto.RoomId,
                dto.RoomName,
                dto.Name,
                dto.Description,
                dto.MaxAttempts,
                dto.CreatorId,
                dto.CreatorUsername,
                dto.CreatedAt,
                dto.UpdaterId,
                dto.UpdaterUsername,
                dto.UpdatedAt,
                [
                    .. dto.Artifacts.Select(a => new ArtifactResponse(
                        a.ChallengeId,
                        a.FileName,
                        a.FileSize,
                        a.UploaderId,
                        a.ContentType,
                        a.CreatedAt
                    )),
                ],
                dto.FlagsCount,
                dto.Tags
            );

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "challenges/{id:guid}",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var query = new Query(id, claims.GetLoggedInUserId());

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Challenges));
        }
    }
}
