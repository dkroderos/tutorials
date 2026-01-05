namespace Ctf.Api.Repositories.Users;

public interface IUserRepository
{
    Task<Guid> CreateAsync(
        CreateUserDto createUserDto,
        CreateUserProviderDto? createUserProviderDto = null
    );
    Task<bool> EmailExistsAsync(string email);
    Task<bool> ExistsAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<Guid?> GetIdByExternalProviderAsync(ExternalProvider provider, string providerId);
    Task<string?> GetUsernameByIdAsync(Guid id);
    Task<bool> UsernameExistsAsync(string username);
}
