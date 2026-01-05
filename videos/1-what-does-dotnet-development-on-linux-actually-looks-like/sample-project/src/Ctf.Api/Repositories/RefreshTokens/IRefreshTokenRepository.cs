namespace Ctf.Api.Repositories.RefreshTokens;

public interface IRefreshTokenRepository
{
    Task AddAsync(AddRefreshTokenDto dto);
    Task<Guid?> GetUserIdByTokenAsync(string token);
    Task<bool> IsValidAsync(string token, Guid userId);
    Task RemoveAsync(string token, Guid userId);
    Task RemoveAllAsync(Guid userId);
}
