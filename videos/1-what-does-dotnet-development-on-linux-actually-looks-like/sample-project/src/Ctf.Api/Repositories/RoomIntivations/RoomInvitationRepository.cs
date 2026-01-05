using Ctf.Api.Repositories.Rooms;
using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.RoomIntivations;

public sealed class RoomInvitationRepository(NpgsqlDataSource dataSource)
    : IRoomInvitationRepository
{
    public async Task<bool> AcceptAsync(Guid roomId, Guid inviteeId)
    {
        const string deleteInvitationSql = """
            DELETE FROM room_invitations
            WHERE room_id = @RoomId AND invitee_id = @InviteeId
            RETURNING room_role;
            """;

        const string addMemberSql = """
            INSERT INTO room_members (room_id, user_id, room_role)
            VALUES (@RoomId, @InviteeId, @Role::room_role);
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var roleString = await db.QueryFirstOrDefaultAsync<string>(
            deleteInvitationSql,
            new { RoomId = roomId, InviteeId = inviteeId },
            transaction: tx
        );

        if (!Enum.TryParse<RoomRole>(roleString, ignoreCase: true, out var role))
        {
            await tx.RollbackAsync();
            return false;
        }

        await db.ExecuteAsync(
            addMemberSql,
            new
            {
                RoomId = roomId,
                InviteeId = inviteeId,
                Role = role.ToString()!.ToLower(),
            },
            transaction: tx
        );

        await tx.CommitAsync();
        return true;
    }

    public async Task CreateAsync(CreateRoomInvitationDto dto)
    {
        const string sql = """
            INSERT INTO room_invitations (
                room_id,
                invitee_id,
                inviter_id,
                room_role
            ) VALUES (
                @RoomId,
                @InviteeId,
                @InviterId,
                @InviteeRole::room_role
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();

        await db.ExecuteAsync(
            sql,
            new
            {
                dto.RoomId,
                dto.InviteeId,
                dto.InviterId,
                InviteeRole = dto.InviteeRole.ToString().ToLower(),
            }
        );
    }

    public async Task<bool> DeleteAsync(Guid roomId, Guid inviteeId)
    {
        const string sql = """
            DELETE FROM room_invitations
            WHERE room_id = @RoomId AND invitee_id = @InviteeId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var rows = await db.ExecuteAsync(sql, new { RoomId = roomId, InviteeId = inviteeId });

        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid roomId, Guid userId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 
                FROM room_invitations
                WHERE room_id = @RoomId AND user_id = @UserId
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { RoomId = roomId, UserId = userId });
    }

    public async Task<PagedList<ReceivedRoomInvitationDto>> GetReceivedAsync(
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
            "roomname" => "RoomName",
            _ => "InvitedAt",
        };

        const string sql = """
            SELECT
                ri.room_id AS RoomId,
                r.name AS RoomName,
                ri.inviter_id AS InviterId,
                u.username AS InviterUsername,
                INITCAP(ri.room_role::text) AS RoomRole,
                ri.invited_at AS InvitedAt
            FROM room_invitations ri
            INNER JOIN rooms r ON r.id = ri.room_id
            INNER JOIN users u ON u.id = ri.inviter_id
            WHERE 
                ri.invitee_id = @UserId
                AND (
                    @SearchTerm IS NULL OR
                    r.name ILIKE '%' || @SearchTerm || '%' OR
                    u.username ILIKE '%' || @SearchTerm || '%'
                )
            """;

        return await PagedList<ReceivedRoomInvitationDto>.CreateAsync(
            dataSource,
            sql,
            sortColumn,
            isAscending,
            page,
            pageSize,
            new { UserId = userId, SearchTerm = searchTerm }
        );
    }

    public Task<IEnumerable<ReceivedRoomInvitationDto>> GetSentAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}
