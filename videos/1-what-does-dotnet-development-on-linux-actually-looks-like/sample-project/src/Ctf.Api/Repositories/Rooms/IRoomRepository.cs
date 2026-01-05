namespace Ctf.Api.Repositories.Rooms;

public interface IRoomRepository
{
    Task<bool?> IsAllowedForPlayersToViewOtherTeamSolves(Guid roomId);
    Task<bool?> AllowsPlayerCreatedTeamsAsync(Guid roomId);
    Task<bool?> AreChallengesHiddenAsync(Guid roomId);
    Task<Guid> CreateAsync(CreateRoomDto dto);
    Task<bool> ExistsByNameAsync(Guid userId, string name);
    Task<RoomSolveRequirementsDto?> GetRoomSolveRequirementsAsync(Guid roomId);
    Task<PagedList<RoomDto>> QueryAsync(
        Guid userId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
}
