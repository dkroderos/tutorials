using System.Security.Claims;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.Rooms;

public static class GetRooms
{
    public sealed record Query(
        Guid UserId,
        string? SearchTerm,
        string? SortBy,
        bool IsAscending,
        int Page,
        int PageSize
    );

    public sealed record Response(
        Guid Id,
        string Name,
        string Description,
        DateTime JoinedAt,
        RoomRole RoomRole
    );

    public sealed class Handler(IRoomRepository roomRepository) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var dtos = await roomRepository.QueryAsync(
                request.UserId,
                request.SearchTerm,
                request.SortBy,
                request.IsAscending,
                request.Page,
                request.PageSize
            );

            var response = PagedList<Response>.Create(
                [
                    .. dtos.Items.Select(dto => new Response(
                        dto.Id,
                        dto.Name,
                        dto.Description,
                        dto.JoinedAt,
                        dto.RoomRole
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
                    "rooms",
                    async (
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
                .WithTags(nameof(Rooms));
        }
    }
}
