using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.Repositories.RoomIntivations;

public sealed record CreateRoomInvitationDto
{
    public required Guid RoomId { get; init; }
    public required Guid InviteeId { get; init; }
    public required Guid InviterId { get; init; }
    public required RoomRole InviteeRole { get; init; }
}

public sealed record ReceivedRoomInvitationDto
{
    public required Guid RoomId { get; init; }
    public required string RoomName { get; init; }
    public required Guid InviterId { get; init; }
    public required string InviterUsername { get; init; }
    public required RoomRole RoomRole { get; init; }
    public required DateTime InvitedAt { get; init; }
}
