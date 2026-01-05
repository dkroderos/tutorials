using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Repositories.RoomMembers;

public sealed record RoomMemberDto
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required RoomRole RoomRole { get; init; }
    public required DateTime JoinedAt { get; init; }
}
