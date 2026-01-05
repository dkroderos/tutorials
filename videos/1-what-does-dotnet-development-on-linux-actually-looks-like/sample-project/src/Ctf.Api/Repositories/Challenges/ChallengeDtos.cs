using Ctf.Api.Repositories.Artifacts;

namespace Ctf.Api.Repositories.Challenges;

public sealed record ChallengeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required int MaxAttempts { get; init; }
    public required string[] Tags { get; init; }
}

public sealed record ChallengeDetailsDto
{
    public required Guid Id { get; init; }
    public required Guid RoomId { get; init; }
    public required string RoomName { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MaxAttempts { get; init; }
    public required Guid CreatorId { get; init; }
    public required string CreatorUsername { get; init; }
    public required DateTime CreatedAt { get; init; }
    public Guid? UpdaterId { get; init; }
    public required string? UpdaterUsername { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public required int FlagsCount { get; init; }
    public required ArtifactDto[] Artifacts { get; init; }
    public required string[] Tags { get; init; }
}

public sealed record CreateChallengeDto
{
    public required Guid RoomId { get; init; }
    public required Guid CreatorId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MaxAttempts { get; init; }
    public required string[] Flags { get; init; }
    public required string[] Tags { get; init; }
}

public sealed record UpdateChallengeDto
{
    public required Guid Id { get; init; }
    public required Guid UpdaterId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MaxAttempts { get; init; }
    public required string[] Flags { get; init; }
    public required string[] Tags { get; init; }
}
