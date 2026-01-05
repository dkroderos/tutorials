namespace Ctf.Api.Repositories.Challenges;

public interface IChallengeRepository
{
    Task<Guid> CreateAsync(CreateChallengeDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<ChallengeDetailsDto?> GetDetailsAsync(Guid id);
    Task<Guid?> GetRoomIdAsync(Guid id);
    Task<bool> NameInRoomExistsAsync(string name, Guid roomId);
    Task<PagedList<ChallengeDto>> QueryAsync(
        Guid roomId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
    Task<bool> UpdateAsync(UpdateChallengeDto dto);
}
