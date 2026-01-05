using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;

namespace Ctf.Api.Features.Solves;

public static class GetChallengeStatus
{
    public sealed record Query(Guid UserId, Guid ChallengeId);

    public sealed record Response(ChallengeStatus Status);

    public enum ChallengeStatus
    {
        NotAPlayer,
        Disabled,
        NoTeam,
        AlreadySolved,
        NotSolved,
    }

    public sealed class Handler(
        IChallengeRepository challengeRepository,
        IRoomMemberRepository roomMemberRepository,
        IRoomRepository roomRepository,
        ITeamMemberRepository teamMemberRepository,
        ISolveRepository solveRepository
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Query request)
        {
            var now = DateTime.UtcNow;

            var roomId = await challengeRepository.GetRoomIdAsync(request.ChallengeId);
            if (roomId is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var roomSolveRequirementsDto = await roomRepository.GetRoomSolveRequirementsAsync(
                roomId.Value
            );
            if (roomSolveRequirementsDto is null || roomSolveRequirementsDto.AreChallengesHidden)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (role is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            if (role != RoomRole.Player)
                return new Response(ChallengeStatus.NotAPlayer);

            if (
                roomSolveRequirementsDto.IsSubmissionsForceDisabled
                || now < roomSolveRequirementsDto.SubmissionStart
                || now > roomSolveRequirementsDto.SubmissionEnd
            )
                return new Response(ChallengeStatus.Disabled);

            var playerTeamId = await teamMemberRepository.GetUserTeamAsync(
                roomId.Value,
                request.UserId
            );

            if (playerTeamId is null)
                return new Response(ChallengeStatus.NoTeam);

            var alreadySolved = await solveRepository.ExistsAsync(
                request.UserId,
                playerTeamId.Value
            );
            if (alreadySolved)
                return new Response(ChallengeStatus.AlreadySolved);

            return new Response(ChallengeStatus.NotSolved);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "challenges/{id:guid}/status",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var query = new Query(claims.GetLoggedInUserId(), id);

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Solves));
        }
    }
}
