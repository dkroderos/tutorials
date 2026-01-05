namespace Ctf.Api.Repositories.Teams;

public sealed record TeamDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed record CreateTeamDto
{
    public required Guid RoomId { get; init; }
    public required string Name { get; init; }
}
