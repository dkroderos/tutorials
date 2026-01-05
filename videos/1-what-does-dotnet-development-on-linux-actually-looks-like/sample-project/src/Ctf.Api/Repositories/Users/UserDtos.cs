namespace Ctf.Api.Repositories.Users;

public sealed record CreateUserDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string? PasswordHash { get; init; }
    public required bool IsVerified { get; init; }
    public required string RegistrationIp { get; init; }
}

public sealed record CreateUserProviderDto
{
    public required ExternalProvider Provider { get; init; }
    public required string ProviderId { get; init; }
}

public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string? PasswordHash { get; init; }
    public required bool IsVerified { get; init; }
}

public enum ExternalProvider
{
    Google,
}
