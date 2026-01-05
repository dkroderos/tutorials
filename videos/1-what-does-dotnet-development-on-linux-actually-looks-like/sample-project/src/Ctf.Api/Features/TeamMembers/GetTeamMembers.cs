using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.TeamMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.Features.TeamMembers;

public static class GetTeamMembers
{
    public sealed record Query(Guid UserId, Guid TeamId);

    public sealed record Response(Guid UserId, string Username, DateTime JoinedAt);

    public sealed class Handler(
        ITeamRepository teamRepository,
        IRoomMemberRepository roomMemberRepository,
        ITeamMemberRepository teamMemberRepository
    ) : IFeature
    {
        public async Task<Result<Response[]>> Handle(Query request)
        {
            var roomId = await teamRepository.GetRoomIdAsync(request.TeamId);
            if (roomId is null)
                return Result.Failure<Response[]>(TeamErrors.NotFound);

            var userRole = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (userRole is null)
                return Result.Failure<Response[]>(TeamErrors.NotFound);

            var dtos = await teamMemberRepository.QueryAsync(request.TeamId);

            var response = dtos.Select(dto => new Response(dto.UserId, dto.Username, dto.JoinedAt))
                .ToArray();

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "teams/{id:guid}",
                    async (Guid id, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var query = new Query(claims.GetLoggedInUserId(), id);

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(TeamMembers));
        }
    }
}
