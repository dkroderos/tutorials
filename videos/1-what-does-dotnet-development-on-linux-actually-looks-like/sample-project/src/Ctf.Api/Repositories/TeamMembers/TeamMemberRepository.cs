using Ctf.Api.Features.TeamMembers;
using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.TeamMembers;

public sealed class TeamMemberRepository(NpgsqlDataSource dataSource) : ITeamMemberRepository
{
    public async Task AddAsync(AddTeamMemberDto dto)
    {
        const string sql = """
            INSERT INTO team_members (team_id, user_id, room_id)
            VALUES (@TeamId, @UserId, @RoomId)
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await db.ExecuteAsync(
            sql,
            new
            {
                dto.TeamId,
                dto.UserId,
                dto.RoomId,
            }
        );
    }

    public async Task<Guid?> GetUserTeamAsync(Guid roomId, Guid userId)
    {
        const string sql = """
            SELECT team_id AS TeamId
            FROM team_members
            WHERE room_id = @RoomId AND user_id = @UserId
            """;

        await using var db = await dataSource.OpenConnectionAsync();

        var teamId = await db.QuerySingleOrDefaultAsync<Guid>(
            sql,
            new { RoomId = roomId, UserId = userId }
        );

        return teamId;
    }

    public async Task<IEnumerable<TeamMemberDto>> QueryAsync(Guid teamId)
    {
        const string sql = """
            SELECT
                tm.user_id AS UserId,
                u.username AS Username,
                tm.joined_at AS JoinedAt
            FROM team_members tm
            INNER JOIN users u ON tm.user_id = u.id
            WHERE tm.team_id = @TeamId
            ORDER BY tm.joined_at
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var dtos = await db.QueryAsync<TeamMemberDto>(sql, new { TeamId = teamId });

        return dtos;
    }

    public async Task<bool> RemoveAsync(
        Guid teamId,
        Guid userId,
        bool deleteTeamIfNoMembers = false
    )
    {
        const string deleteMemberSql = """
            DELETE FROM team_members
            WHERE team_id = @TeamId AND user_id = @UserId
            """;

        const string countMembersSql = """
            SELECT COUNT(*) FROM team_members
            WHERE team_id = @TeamId
            """;

        const string deleteTeamSql = """
            DELETE FROM teams
            WHERE id = @TeamId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var rows = await db.ExecuteAsync(
            deleteMemberSql,
            new { TeamId = teamId, UserId = userId },
            transaction: tx
        );

        if (rows == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        if (!deleteTeamIfNoMembers)
        {
            await tx.CommitAsync();
            return true;
        }

        var memberCount = await db.ExecuteScalarAsync<int>(
            countMembersSql,
            new { TeamId = teamId },
            transaction: tx
        );

        if (memberCount == 0)
            await db.ExecuteAsync(deleteTeamSql, new { TeamId = teamId }, transaction: tx);

        await tx.CommitAsync();
        return true;
    }
}
