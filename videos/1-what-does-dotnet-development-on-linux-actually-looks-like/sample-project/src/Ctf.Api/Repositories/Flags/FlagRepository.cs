using Npgsql;

namespace Ctf.Api.Repositories.Flags;

public sealed class FlagRepository(NpgsqlDataSource dataSource) : IFlagRepository
{
    public async Task<string[]> GetByChallengeIdAsync(Guid challengeId)
    {
        const string sql = """
            SELECT value
            FROM flags
            WHERE challenge_id = @ChallengeId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var cmd = db.CreateCommand();

        cmd.CommandText = sql;
        cmd.Parameters.Add(new NpgsqlParameter("ChallengeId", challengeId));

        var flags = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            flags.Add(reader.GetString(reader.GetOrdinal("value")));
        }

        return [.. flags];
    }
}
