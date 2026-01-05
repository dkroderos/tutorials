using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.RoomMembers;

public static class GetRoomMembers
{
    public sealed record Query(
        Guid UserId,
        Guid RoomId,
        string? SearchTerm,
        string? SortBy,
        bool IsAscending,
        int Page,
        int PageSize
    );

    public sealed record Response(
        Guid UserId,
        string Username,
        RoomRole RoomRole,
        DateTime JoinedAt
    );

    public sealed class Handler(IRoomMemberRepository roomMemberRepository) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var role = await roomMemberRepository.GetRoleAsync(request.RoomId, request.UserId);
            if (role is null || role < RoomRole.Admin)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            var dtos = await roomMemberRepository.QueryAsync(
                request.RoomId,
                request.SearchTerm,
                request.SortBy,
                request.IsAscending,
                request.Page,
                request.PageSize
            );

            var response = PagedList<Response>.Create(
                [
                    .. dtos.Items.Select(dto => new Response(
                        dto.UserId,
                        dto.Username,
                        dto.RoomRole,
                        dto.JoinedAt
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
                    "rooms/{id:guid}/members",
                    async (
                        Guid id,
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
                            id,
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
                .WithTags(nameof(RoomMembers));
        }
    }
}
