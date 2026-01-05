namespace Ctf.Api.Features.TeamMembers;

public sealed record AddTeamMemberDto
{
    public required Guid TeamId { get; init; }
    public required Guid UserId { get; init; }
    public required Guid RoomId { get; init; }
}
