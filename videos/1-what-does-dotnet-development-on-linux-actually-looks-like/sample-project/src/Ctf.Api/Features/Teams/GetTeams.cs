using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.Features.Teams;

public static class GetTeams
{
    public sealed record Query(
        Guid RoomId,
        Guid UserId,
        string? SearchTerm,
        string? SortBy,
        bool IsAscending,
        int Page,
        int PageSize
    );

    public sealed record Response(Guid Id, string Name, DateTime CreatedAt);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        ITeamRepository teamRepository
    ) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var userInRoom = await roomMemberRepository.ExistsAsync(request.RoomId, request.UserId);
            if (!userInRoom)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            var dtos = await teamRepository.QueryAsync(
                request.RoomId,
                request.SearchTerm,
                request.SortBy,
                request.IsAscending,
                request.Page,
                request.PageSize
            );

            var response = PagedList<Response>.Create(
                [.. dtos.Items.Select(dto => new Response(dto.Id, dto.Name, dto.CreatedAt))],
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
                    "teams",
                    async (
                        Guid roomId,
                        string? searchTerm,
                        string? sortBy,
                        bool? sortOrder,
                        int? page,
                        int? pageSize,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var query = new Query(
                            roomId,
                            claims.GetLoggedInUserId(),
                            searchTerm,
                            sortBy,
                            sortOrder ?? true,
                            page.ToValidPage(),
                            pageSize.ToValidPageSize()
                        );

                        var result = await handler.Handle(query);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Teams));
        }
    }
}
