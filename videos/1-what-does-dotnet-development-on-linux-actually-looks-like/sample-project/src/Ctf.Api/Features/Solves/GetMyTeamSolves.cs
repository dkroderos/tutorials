using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;

namespace Ctf.Api.Features.Solves;

public static class GetMyTeamSolves
{
    public sealed record Query(
        Guid QuerierId,
        Guid RoomId,
        string? SearchTerm,
        string? SortBy,
        bool IsAscending,
        int Page,
        int PageSize
    );

    public sealed record Response(Guid ChallengeId, string ChallengeName, DateTime SolvedAt);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        ITeamMemberRepository teamMemberRepository,
        ISolveRepository solveRepository
    ) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var exists = await roomMemberRepository.ExistsAsync(request.RoomId, request.QuerierId);
            if (!exists)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            var teamId = await teamMemberRepository.GetUserTeamAsync(
                request.RoomId,
                request.QuerierId
            );

            if (teamId is null)
                return Result.Failure<PagedList<Response>>(TeamMemberErrors.LoggedUserNotInTheTeam);

            var dtos = await solveRepository.QueryTeamSolvesAsync(
                request.RoomId,
                teamId.Value,
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
                    "rooms/{roomid:guid}/solves",
                    async (
                        Guid roomId,
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
