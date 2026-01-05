using Ctf.Api.Features.TeamMembers;

namespace Ctf.Api.Repositories.TeamMembers;

public interface ITeamMemberRepository
{
    Task AddAsync(AddTeamMemberDto dto);
    Task<Guid?> GetUserTeamAsync(Guid roomId, Guid userId);
    Task<IEnumerable<TeamMemberDto>> QueryAsync(Guid teamId);
    Task<bool> RemoveAsync(Guid teamId, Guid userId, bool deleteTeamIfNoMembers = false);
}
