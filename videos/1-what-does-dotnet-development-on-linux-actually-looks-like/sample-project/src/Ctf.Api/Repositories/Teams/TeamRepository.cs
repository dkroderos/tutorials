using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.Teams;

public sealed class TeamRepository(NpgsqlDataSource dataSource) : ITeamRepository
{
    public async Task<Guid> CreateAsync(CreateTeamDto dto, Guid? firstMemberId = null)
    {
        const string insertTeamSql = """
            INSERT INTO teams (id, room_id, name)
            VALUES (@Id, @RoomId, @Name);
            """;

        const string insertMemberSql = """
            INSERT INTO team_members (team_id, user_id, room_id)
            VALUES (@TeamId, @UserId, @RoomId);
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var id = Guid.CreateVersion7();
        await db.ExecuteAsync(
            insertTeamSql,
            new
            {
                Id = id,
                dto.RoomId,
                dto.Name,
            },
            transaction: tx
        );

        if (firstMemberId is not null)
            await db.ExecuteAsync(
                insertMemberSql,
                new
                {
                    TeamId = id,
                    UserId = firstMemberId.Value,
                    dto.RoomId,
                },
                transaction: tx
            );

        await tx.CommitAsync();

        return id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = """
            DELETE FROM teams
            WHERE id = @Id
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var rows = await db.ExecuteAsync(sql, new { Id = id });

        return rows > 0;
    }

    public async Task<Guid?> GetRoomIdAsync(Guid id)
    {
        const string sql = """
            SELECT room_id
            FROM teams
            WHERE id = @Id
            LIMIT 1
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var dto = await db.QuerySingleOrDefaultAsync<Guid?>(sql, new { Id = id });

        return dto;
    }

    public async Task<bool> NameInRoomExistsAsync(Guid roomId, string name)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM teams
                WHERE room_id = @RoomId AND name = @Name
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { RoomId = roomId, Name = name });
    }

    public async Task<PagedList<TeamDto>> QueryAsync(
        Guid roomId,
        string? searchTerm,
        string? sortBy,
        bool isAscending,
        int page,
        int pageSize
    )
    {
        var sortColumn = sortBy?.ToLowerInvariant() switch
        {
            "name" => "Name",
            "createdat" => "CreatedAt",
            _ => "Name",
        };

        const string sql = """
            SELECT
                id as Id,
                name as NAME,
                created_at AS CreatedAt
            FROM teams
            WHERE
                room_id = @RoomId
                AND (
                    @SearchTerm IS NULL OR
                    name ILIKE '%' || @SearchTerm || '%'
                )
            """;

        return await PagedList<TeamDto>.CreateAsync(
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
