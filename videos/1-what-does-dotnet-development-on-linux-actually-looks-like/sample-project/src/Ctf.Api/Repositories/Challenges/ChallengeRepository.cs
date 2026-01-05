using Ctf.Api.Repositories.Artifacts;
using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.Challenges;

public sealed class ChallengeRepository(NpgsqlDataSource dataSource) : IChallengeRepository
{
    public async Task<Guid> CreateAsync(CreateChallengeDto dto)
    {
        const string createChallengeSql = """
            INSERT INTO challenges (id, room_id, creator_id, name, description, max_attempts)
            VALUES (@Id, @RoomId, @CreatorId, @Name, @Description, @MaxAttempts)
            """;
        const string insertFlagSql = """
            INSERT INTO flags (challenge_id, value)
            VALUES (@ChallengeId, @Value)
            ON CONFLICT DO NOTHING
            """;
        const string insertTagSql = """
            INSERT INTO tags (challenge_id, value)
            VALUES (@ChallengeId, @Value)
            ON CONFLICT DO NOTHING
            """;

        var id = Guid.CreateVersion7();

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        await db.ExecuteAsync(
            createChallengeSql,
            new
            {
                Id = id,
                dto.RoomId,
                dto.CreatorId,
                dto.Name,
                dto.Description,
                dto.MaxAttempts,
            },
            transaction: tx
        );

        foreach (var flag in dto.Flags)
            await db.ExecuteAsync(
                insertFlagSql,
                new { ChallengeId = id, Value = flag },
                transaction: tx
            );
        foreach (var tag in dto.Tags)
            await db.ExecuteAsync(
                insertTagSql,
                new { ChallengeId = id, Value = tag },
                transaction: tx
            );

        await tx.CommitAsync();
        return id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = """
            DELETE FROM challenges
            WHERE id = @Id
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var rows = await db.ExecuteAsync(sql, new { Id = id });

        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM challenges
                WHERE id = @Id
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    public async Task<ChallengeDetailsDto?> GetDetailsAsync(Guid id)
    {
        const string sql = """
            SELECT
                c.id, c.room_id, r.name AS room_name, c.name, c.description, c.max_attempts,
                c.creator_id, cu.username AS creator_username, c.created_at,
                c.updater_id, uu.username AS updater_username, c.updated_at,
                COALESCE(f.flag_count, 0) AS flags_count,
                COALESCE(t.tags, ARRAY[]::text[]) AS tags,
                a.file_name, a.file_size, a.content_type, a.uploader_id, a.created_at AS artifact_created_at
            FROM challenges c
            INNER JOIN rooms r ON r.id = c.room_id
            LEFT JOIN users cu ON cu.id = c.creator_id
            LEFT JOIN users uu ON uu.id = c.updater_id
            LEFT JOIN (
                SELECT challenge_id, COUNT(*) AS flag_count
                FROM flags
                GROUP BY challenge_id
            ) f ON f.challenge_id = c.id
            LEFT JOIN (
                SELECT challenge_id, ARRAY_AGG(value ORDER BY value) AS tags
                FROM tags
                GROUP BY challenge_id
            ) t ON t.challenge_id = c.id
            LEFT JOIN artifacts a ON a.challenge_id = c.id
            WHERE c.id = @Id
            LIMIT 1
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var cmd = db.CreateCommand();

        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("Id", id));

        await using var reader = await cmd.ExecuteReaderAsync();

        ChallengeDetailsDto? challenge = null;
        var artifacts = new List<ArtifactDto>();

        while (await reader.ReadAsync())
        {
            challenge ??= new ChallengeDetailsDto
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                RoomId = reader.GetGuid(reader.GetOrdinal("room_id")),
                RoomName = reader.GetString(reader.GetOrdinal("room_name")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                MaxAttempts = reader.GetInt32(reader.GetOrdinal("max_attempts")),
                CreatorId = reader.GetGuid(reader.GetOrdinal("creator_id")),
                CreatorUsername = reader.GetString(reader.GetOrdinal("creator_username")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdaterId = reader.IsDBNull(reader.GetOrdinal("updater_id"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("updater_id")),
                UpdaterUsername = reader.IsDBNull(reader.GetOrdinal("updater_username"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("updater_username")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("updated_at")),
                FlagsCount = reader.GetInt32(reader.GetOrdinal("flags_count")),
                Tags = reader.IsDBNull(reader.GetOrdinal("tags"))
                    ? []
                    : reader.GetFieldValue<string[]>(reader.GetOrdinal("tags")),
                Artifacts = [], // will be set later
            };

            if (!reader.IsDBNull(reader.GetOrdinal("file_name")))
                artifacts.Add(
                    new ArtifactDto
                    {
                        ChallengeId = id,
                        FileName = reader.GetString(reader.GetOrdinal("file_name")),
                        FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
                        ContentType = reader.GetString(reader.GetOrdinal("content_type")),
                        UploaderId = reader.IsDBNull(reader.GetOrdinal("uploader_id"))
                            ? null
                            : reader.GetGuid(reader.GetOrdinal("uploader_id")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("artifact_created_at")),
                    }
                );
        }

        if (challenge is not null)
            challenge = challenge with { Artifacts = [.. artifacts] };

        return challenge;
    }

    public async Task<Guid?> GetRoomIdAsync(Guid id)
    {
        const string sql = """
            SELECT room_id
            FROM challenges
            WHERE id = @Id
            LIMIT 1
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var dto = await db.QuerySingleOrDefaultAsync<Guid?>(sql, new { Id = id });

        return dto;
    }

    public async Task<bool> NameInRoomExistsAsync(string name, Guid roomId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM challenges
                WHERE name = @Name AND room_id = @RoomId
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { Name = name, RoomId = roomId });
    }

    public async Task<PagedList<ChallengeDto>> QueryAsync(
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
            _ => "Id",
        };

        const string sql = """
            SELECT
                c.id AS Id,
                c.name AS Name,
                c.created_at AS CreatedAt,
                c.max_attempts AS MaxAttempts,
                COALESCE(ARRAY_AGG(DISTINCT t.value) FILTER (WHERE t.value IS NOT NULL), ARRAY[]::text[]) AS Tags
            FROM challenges c
            LEFT JOIN tags t ON t.challenge_id = c.id
            WHERE 
                c.room_id = @RoomId
                AND (
                    @SearchTerm IS NULL OR
                    c.name ILIKE '%' || @SearchTerm || '%' OR
                    t.value ILIKE '%' || @SearchTerm || '%'
                )
            GROUP BY c.id, c.name, c.created_at, c.max_attempts
            """;

        return await PagedList<ChallengeDto>.CreateAsync(
            dataSource,
            sql,
            sortColumn,
            isAscending,
            page,
            pageSize,
            new { RoomId = roomId, SearchTerm = searchTerm }
        );
    }

    public async Task<bool> UpdateAsync(UpdateChallengeDto dto)
    {
        const string updateChallengeSql = """
            UPDATE challenges
            SET
                name = @Name,
                description = @Description,
                max_attempts = @MaxAttempts,
                updater_id = @UpdaterId,
                updated_at = NOW()
            WHERE id = @Id
            """;
        const string deleteFlagsSql = """
            DELETE FROM flags WHERE challenge_id = @ChallengeId
            """;
        const string insertFlagSql = """
            INSERT INTO flags (challenge_id, value)
            VALUES (@ChallengeId, @Value)
            ON CONFLICT DO NOTHING
            """;
        const string deleteTagsSql = """
            DELETE FROM tags WHERE challenge_id = @ChallengeId
            """;
        const string insertTagSql = """
            INSERT INTO tags (challenge_id, value)
            VALUES (@ChallengeId, @Value)
            ON CONFLICT DO NOTHING
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var rows = await db.ExecuteAsync(
            updateChallengeSql,
            new
            {
                dto.Id,
                dto.Name,
                dto.Description,
                dto.MaxAttempts,
                dto.UpdaterId,
            },
            tx
        );

        await db.ExecuteAsync(deleteFlagsSql, new { ChallengeId = dto.Id }, transaction: tx);
        foreach (var flag in dto.Flags)
            await db.ExecuteAsync(
                insertFlagSql,
                new { ChallengeId = dto.Id, Value = flag },
                transaction: tx
            );

        await db.ExecuteAsync(deleteTagsSql, new { ChallengeId = dto.Id }, transaction: tx);
        foreach (var tag in dto.Tags)
            await db.ExecuteAsync(
                insertTagSql,
                new { ChallengeId = dto.Id, Value = tag },
                transaction: tx
            );

        await tx.CommitAsync();
        return rows > 0;
    }
}
