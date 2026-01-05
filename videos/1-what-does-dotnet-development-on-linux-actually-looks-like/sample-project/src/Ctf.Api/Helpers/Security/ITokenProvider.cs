namespace Ctf.Api.Helpers.Security;

public interface ITokenProvider
{
    string CreateAccessToken(Guid userId, string username);
    string CreateRefreshToken();
}
