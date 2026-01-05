using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Features.Challenges;

public static class GetChallenges
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

    public sealed record Response(
        Guid Id,
        string Name,
        DateTime CreatedAt,
        int MaxAttempts,
        string[] Tags
    );

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IRoomRepository roomRepository,
        IChallengeRepository challengeRepository
    ) : IFeature
    {
        public async Task<Result<PagedList<Response>>> Handle(Query request)
        {
            var isMember = await roomMemberRepository.ExistsAsync(request.RoomId, request.UserId);

            if (!isMember)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            var areChallengesHidden = await roomRepository.AreChallengesHiddenAsync(request.RoomId);
            if (areChallengesHidden is null)
                return Result.Failure<PagedList<Response>>(RoomErrors.NotFound);

            if (areChallengesHidden.Value)
                return PagedList<Response>.Empty();

            var dtos = await challengeRepository.QueryAsync(
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
                        dto.Id,
                        dto.Name,
                        dto.CreatedAt,
                        dto.MaxAttempts,
                        dto.Tags
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
                    "rooms/{roomId:guid}/challenges",
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
                            roomId,
                            claims.GetLoggedInUserId(),
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
                .WithTags(nameof(Challenges));
        }
    }
}
