namespace Ctf.Api.Repositories.Teams;

public interface ITeamRepository
{
    Task<Guid> CreateAsync(CreateTeamDto dto, Guid? firstMemberId = null);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> NameInRoomExistsAsync(Guid roomId, string name);
    Task<Guid?> GetRoomIdAsync(Guid id);
    Task<PagedList<TeamDto>> QueryAsync(
        Guid roomId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
}
