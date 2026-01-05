using System.Security.Claims;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.RoomInvitations;

public static class GetReceivedInvites
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
        Guid RoomId,
        string RoomName,
        Guid InviterId,
        string InviterUsername,
        RoomRole RoomRole,
        DateTime InvitedAt
    );

    public sealed class Handler(IRoomInvitationRepository roomInvitationRepository) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var dtos = await roomInvitationRepository.GetReceivedAsync(
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
                        dto.RoomId,
                        dto.RoomName,
                        dto.InviterId,
                        dto.InviterUsername,
                        dto.RoomRole,
                        dto.InvitedAt
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
                    "invites/received",
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
                .WithTags(nameof(RoomInvitations));
        }
    }
}
