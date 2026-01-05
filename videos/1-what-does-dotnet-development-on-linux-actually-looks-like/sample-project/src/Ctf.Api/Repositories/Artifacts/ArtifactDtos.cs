namespace Ctf.Api.Repositories.Artifacts;

public sealed record ArtifactDto
{
    public required Guid ChallengeId { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required Guid? UploaderId { get; init; }
    public required string ContentType { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed record ArtifactStreamDto
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream Data { get; init; }
}

public sealed record AddArtifactDto
{
    public required Guid ChallengeId { get; init; }
    public required Guid UploaderId { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
    public required string ContentType { get; init; }
    public required Stream Data { get; init; }
}
