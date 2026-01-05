namespace Ctf.Api.Repositories.Solves;

public interface ISolveRepository
{
    Task CreateAsync(CreateSolveDto dto);
    Task<bool> ExistsAsync(Guid challengeId, Guid teamId);
    Task<PagedList<TeamSolveDto>> QueryTeamSolvesAsync(
        Guid roomId,
        Guid teamId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
}
