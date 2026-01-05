namespace Ctf.Api.Repositories.RoomIntivations;

public interface IRoomInvitationRepository
{
    Task<bool> AcceptAsync(Guid roomId, Guid inviteeId);
    Task CreateAsync(CreateRoomInvitationDto dto);
    Task<bool> DeleteAsync(Guid roomId, Guid inviteeId);
    Task<bool> ExistsAsync(Guid roomId, Guid userId);
    Task<PagedList<ReceivedRoomInvitationDto>> GetReceivedAsync(
        Guid userId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    );
    Task<IEnumerable<ReceivedRoomInvitationDto>> GetSentAsync(Guid userId);
}
