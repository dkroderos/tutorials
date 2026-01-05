using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;

namespace Ctf.Api.Features.Solves;

public sealed class GetOtherTeamSolves
{
    public sealed record Query(
        Guid QuerierId,
        Guid RoomId,
        Guid TeamId,
        string? SearchTerm,
        string? SortBy,
        bool IsAscending,
        int Page,
        int PageSize
    );

    public sealed record Response(Guid ChallengeId, string ChallengeName, DateTime SolvedAt);

    public sealed class Handler(
        IRoomRepository roomRepository,
        IRoomMemberRepository roomMemberRepository,
        ISolveRepository solveRepository
    ) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var allowPlayersToViewOtherTeamSolves =
                await roomRepository.IsAllowedForPlayersToViewOtherTeamSolves(request.RoomId);

            if (allowPlayersToViewOtherTeamSolves is null)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(request.RoomId, request.QuerierId);
            if (role is null)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            if (role == RoomRole.Player && !allowPlayersToViewOtherTeamSolves.Value)
                return Result.Failure<PagedList<Response>>(
                    SolveErrors.NotAllowedToViewOtherTeamSolves
                );

            var dtos = await solveRepository.QueryTeamSolvesAsync(
                request.RoomId,
                request.TeamId,
                request.SearchTerm,
                request.SortBy,
                request.IsAscending,
                request.Page,
                request.PageSize
            );

            var response = PagedList<Response>.Create(
                [
                    .. dtos.Items.Select(dto => new Response(
                        dto.ChallengeId,
                        dto.ChallengeName,
                        dto.SolvedAt
                    )),
                ],
                dtos.CurrentPage,
                dtos.PageSize,
                dtos.TotalItems
            );

            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "rooms/{roomid:guid}/solves/{teamid:guid}",
                    async (
                        Guid roomId,
                        Guid teamId,
                        string? searchTerm,
                        string? sortBy,
                        bool? isAscending,
                        int? page,
                        int? pageSize,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var query = new Query(
                            claims.GetLoggedInUserId(),
                            roomId,
                            teamId,
                            searchTerm,
                            sortBy,
                            isAscending ?? false,
                            page.ToValidPage(),
                            pageSize.ToValidPageSize()
                        );

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Solves));
        }
    }
}
