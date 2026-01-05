namespace Ctf.Api.Repositories.Flags;

public interface IFlagRepository
{
    Task<string[]> GetByChallengeIdAsync(Guid challengeId);
}
