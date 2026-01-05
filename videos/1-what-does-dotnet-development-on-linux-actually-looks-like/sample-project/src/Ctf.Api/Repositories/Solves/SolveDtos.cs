namespace Ctf.Api.Repositories.Solves;

public sealed record CreateSolveDto
{
    public required Guid ChallengeId { get; init; }
    public required Guid TeamId { get; init; }
}

public sealed record TeamSolveDto
{
    public required Guid ChallengeId { get; init; }
    public required string ChallengeName { get; init; }
    public required DateTime SolvedAt { get; init; }
}
