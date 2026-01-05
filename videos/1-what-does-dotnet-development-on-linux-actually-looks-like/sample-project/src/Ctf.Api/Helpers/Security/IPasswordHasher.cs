namespace Ctf.Api.Helpers.Security;

public interface IPasswordHasher
{
    public string Hash(string password);

    public bool Verify(string password, string passwordHash);
}
