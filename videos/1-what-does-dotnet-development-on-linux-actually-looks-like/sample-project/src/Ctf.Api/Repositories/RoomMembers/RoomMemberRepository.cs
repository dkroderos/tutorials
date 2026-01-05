using Ctf.Api.Repositories.Rooms;
using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.RoomMembers;

public sealed class RoomMemberRepository(NpgsqlDataSource dataSource) : IRoomMemberRepository
{
    public async Task<bool> DeleteAsync(Guid roomId, Guid userId)
    {
        const string sql = """
            DELETE FROM room_members
            WHERE room_id = @RoomId AND user_id = @UserId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var rows = await db.ExecuteAsync(sql, new { RoomId = roomId, UserId = userId });

        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid roomId, Guid userId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM room_members
                WHERE room_id = @RoomId AND user_id = @UserId
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { RoomId = roomId, UserId = userId });
    }

    public async Task<RoomRole?> GetRoleAsync(Guid roomId, Guid userId)
    {
        const string sql = """
            SELECT room_role
            FROM room_members
            WHERE room_id = @RoomId AND user_id = @UserId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var roleString = await db.QueryFirstOrDefaultAsync<string>(
            sql,
            new { RoomId = roomId, UserId = userId }
        );

        if (!Enum.TryParse<RoomRole>(roleString, ignoreCase: true, out var role))
            return null;

        return role;
    }

    public async Task<PagedList<RoomMemberDto>> QueryAsync(
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
            "name" => "Username",
            "username" => "Username",
            "joinedat" => "JoinedAt",
            _ => "UserId",
        };

        const string sql = """
            SELECT
                rm.user_id AS UserId,
                u.username AS Username,
                rm.room_role AS RoomRole,
                rm.joined_at AS JoinedAt
            FROM room_members rm
            JOIN users u ON rm.user_id = u.id
            WHERE 
                rm.room_id = @RoomId
                AND (
                    @SearchTerm IS NULL OR
                    u.username ILIKE '%' || @SearchTerm || '%'
                )
            """;

        return await PagedList<RoomMemberDto>.CreateAsync(
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
