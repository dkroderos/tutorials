using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.Solves;

public sealed class SolveRepository(NpgsqlDataSource dataSource) : ISolveRepository
{
    public async Task CreateAsync(CreateSolveDto dto)
    {
        const string sql = """
            INSERT INTO solves (challenge_id, team_id)
            VALUES (@ChallengeId, @TeamId)
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await db.ExecuteAsync(sql, new { dto.ChallengeId, dto.TeamId });
    }

    public async Task<bool> ExistsAsync(Guid challengeId, Guid teamId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 
                FROM solves
                WHERE challenge_id = @ChallengeId AND team_id = @TeamId
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();

        return await db.ExecuteScalarAsync<bool>(
            sql,
            new { ChallengeId = challengeId, TeamId = teamId }
        );
    }

    public async Task<PagedList<TeamSolveDto>> QueryTeamSolvesAsync(
        Guid roomId,
        Guid teamId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    )
    {
        var sortColumn = sortBy?.ToLowerInvariant() switch
        {
            "name" => "ChallengeName",
            _ => "SolvedAt",
        };

        const string sql = """
            SELECT 
                s.challenge_id as ChallengeId
                c.challenge_name as ChallengeName
                s.team_id as TeamId
                s.solved_at as SolvedAt
            FROM solves s
            JOIN challenges c ON s.challenge_id = c.id
            WHERE c.room_id = @RoomId
                AND s.team_id = @TeamId
                AND (
                    @SearchTerm IS NULL OR c.name ILIKE '%' || @SearchTerm || '%'
               )
            """;

        return await PagedList<TeamSolveDto>.CreateAsync(
            dataSource,
            sql,
            sortColumn,
            isAscending,
            page,
            pageSize,
            new { RoomId = roomId, SearchTerm = searchTerm }
        );
    }
}
