namespace Ctf.Api.Repositories.Rooms;

public sealed record RoomDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required RoomRole RoomRole { get; init; }
    public required DateTime JoinedAt { get; init; }
}

public sealed record RoomSolveRequirementsDto
{
    public required bool AreChallengesHidden { get; init; }
    public required bool IsSubmissionsForceDisabled { get; init; }
    public required DateTime SubmissionStart { get; init; }
    public required DateTime SubmissionEnd { get; init; }
}

public sealed record CreateRoomDto
{
    public required Guid CreatorId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required bool AreChallengesHidden { get; init; }
    public required bool IsSubmissionsForceDisabled { get; init; }
    public required bool AllowPlayerCreatedTeams { get; init; }
    public required bool AllowPlayersToViewOtherTeamSolves { get; init; }
    public required DateTime SubmissionStart { get; init; }
    public required DateTime SubmissionEnd { get; init; }
}
