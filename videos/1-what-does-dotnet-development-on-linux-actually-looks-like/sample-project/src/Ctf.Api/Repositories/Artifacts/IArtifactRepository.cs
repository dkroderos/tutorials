namespace Ctf.Api.Repositories.Artifacts;

public interface IArtifactRepository
{
    Task AddAsync(AddArtifactDto dto);
    Task<bool> ExistsAsync(Guid challengeId, string fileName);
    Task<bool> DeleteAsync(Guid challengeId, string fileName);
    Task<Stream?> GetStreamAsync(Guid challengeId, string fileName);
}
