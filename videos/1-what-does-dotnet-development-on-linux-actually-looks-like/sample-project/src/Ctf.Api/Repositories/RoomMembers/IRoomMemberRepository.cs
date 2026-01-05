using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Repositories.RoomMembers;

public interface IRoomMemberRepository
{
    Task<bool> DeleteAsync(Guid roomId, Guid userId);
    Task<bool> ExistsAsync(Guid roomId, Guid userId);
    Task<RoomRole?> GetRoleAsync(Guid roomId, Guid userId);
    Task<PagedList<RoomMemberDto>> QueryAsync(
        Guid roomId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
}
