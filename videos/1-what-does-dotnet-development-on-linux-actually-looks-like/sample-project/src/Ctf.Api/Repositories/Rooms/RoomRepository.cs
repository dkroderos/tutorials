using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.Rooms;

public sealed class RoomRepository(NpgsqlDataSource dataSource) : IRoomRepository
{
    public async Task<bool?> IsAllowedForPlayersToViewOtherTeamSolves(Guid roomId)
    {
        const string sql = """
            SELECT allow_players_to_view_other_team_solves
            FROM rooms
            WHERE id = @RoomId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool?>(sql, new { RoomId = roomId });
    }

    public async Task<bool?> AllowsPlayerCreatedTeamsAsync(Guid roomId)
    {
        const string sql = """
            SELECT allow_player_created_teams
            FROM rooms
            WHERE id = @RoomId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool?>(sql, new { RoomId = roomId });
    }

    public async Task<bool?> AreChallengesHiddenAsync(Guid roomId)
    {
        const string sql = """
            SELECT are_challenges_hidden
            FROM rooms
            WHERE id = @RoomId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool?>(sql, new { RoomId = roomId });
    }

    public async Task<Guid> CreateAsync(CreateRoomDto dto)
    {
        const string sql = """
            INSERT INTO rooms (id, creator_id, name, description)
            VALUES (@Id, @CreatorId, @Name, @Description);

            INSERT INTO room_members (room_id, user_id, room_role)
            VALUES (@Id, @CreatorId, @Role::room_role);
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var id = Guid.CreateVersion7();
        await db.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                dto.CreatorId,
                dto.Name,
                dto.Description,
                Role = RoomRole.Owner.ToString().ToLower(),
            },
            transaction: tx
        );

        await tx.CommitAsync();

        return id;
    }

    public async Task<bool> ExistsByNameAsync(Guid userId, string name)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM rooms
                WHERE creator_id = @UserId AND name = @Name
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { UserId = userId, Name = name });
    }

    public async Task<RoomSolveRequirementsDto?> GetRoomSolveRequirementsAsync(Guid roomId)
    {
        const string sql = """
            SELECT 
                are_challenges_hidden as AreChallengesHidden,
                is_submissions_force_disabled as IsSubmissionsForceDisabled,
                submission_start as SubmissionStart,
                submission_end as SubmissionEnd
            FROM rooms
            WHERE id = @RoomId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<RoomSolveRequirementsDto?>(sql, new { RoomId = roomId });
    }

    public async Task<PagedList<RoomDto>> QueryAsync(
        Guid userId,
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
            "joinedat" => "JoinedAt",
            _ => "Name",
        };

        const string sql = """
            SELECT 
                r.id as Id,
                r.name as NAME,
                r.description as Description,
                INITCAP(rm.room_role::text) AS RoomRole,
                rm.joined_at AS JoinedAt
            FROM rooms r
            INNER JOIN room_members rm ON rm.room_id = r.id
            WHERE 
                rm.user_id = @UserId
                AND (
                    @SearchTerm IS NULL OR
                    r.name ILIKE '%' || @SearchTerm || '%' OR
                    r.description ILIKE '%' || @SearchTerm || '%'
                )
            """;

        return await PagedList<RoomDto>.CreateAsync(
            dataSource,
            sql,
            sortColumn,
            isAscending,
            page,
            pageSize,
            new { UserId = userId, SearchTerm = searchTerm }
        );
    }
}
