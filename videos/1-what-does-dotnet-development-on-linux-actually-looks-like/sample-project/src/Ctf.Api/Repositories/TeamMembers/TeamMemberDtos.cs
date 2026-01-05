namespace Ctf.Api.Repositories.TeamMembers;

public sealed record TeamMemberDto
{
    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required DateTime JoinedAt { get; init; }
}
