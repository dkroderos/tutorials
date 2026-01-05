namespace Ctf.Api.Repositories.RefreshTokens;

public sealed record AddRefreshTokenDto
{
    public required string Token { get; init; }
    public required Guid UserId { get; init; }
}
