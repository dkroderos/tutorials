using Dapper;
using Npgsql;

namespace Ctf.Api.Repositories.Users;

public sealed class UserRepository(NpgsqlDataSource dataSource) : IUserRepository
{
    public async Task<Guid> CreateAsync(
        CreateUserDto createUserDto,
        CreateUserProviderDto? createUserProviderDto = null
    )
    {
        const string createUserSql = """
            INSERT INTO users (id, username, email, password_hash, registration_ip)
            VALUES (@UserId, @Username, @Email, @PasswordHash, @RegistrationIp :: INET);
            """;

        const string createUserProviderSql = """
            INSERT INTO user_providers (provider, provider_id, user_id)
            VALUES (@Provider, @ProviderId, @UserId);
            """;

        var userId = Guid.CreateVersion7();

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        await db.ExecuteAsync(
            createUserSql,
            new
            {
                UserId = userId,
                createUserDto.Username,
                createUserDto.Email,
                createUserDto.PasswordHash,
                createUserDto.RegistrationIp,
            },
            transaction: tx
        );

        if (createUserProviderDto is not null)
            await db.ExecuteAsync(
                createUserProviderSql,
                new
                {
                    Provider = createUserProviderDto.Provider.ToString(),
                    createUserProviderDto.ProviderId,
                    UserId = userId,
                },
                transaction: tx
            );

        await tx.CommitAsync();
        return userId;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email)
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { Email = email });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM users WHERE id = @Id)
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT
                id AS Id,
                username AS Username,
                password_hash AS PasswordHash,
                is_verified AS IsVerified
            FROM users
            WHERE email = @Email
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var dto = await db.QuerySingleOrDefaultAsync<UserDto>(sql, new { Email = email });

        return dto;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                id AS Id,
                username AS Username,
                password_hash AS PasswordHash,
                is_verified AS IsVerified
            FROM users
            WHERE id = @Id
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var dto = await db.QuerySingleOrDefaultAsync<UserDto>(sql, new { Id = id });

        return dto;
    }

    public async Task<string?> GetUsernameByIdAsync(Guid id)
    {
        const string sql = """
            SELECT username AS Username
            FROM users
            WHERE id = @Id
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var username = await db.QuerySingleOrDefaultAsync<string>(sql, new { Id = id });

        return username;
    }

    public async Task<Guid?> GetIdByExternalProviderAsync(
        ExternalProvider provider,
        string providerId
    )
    {
        const string sql = """
            SELECT user_id
            FROM user_providers
            WHERE provider = @Provider AND provider_id = @ProviderId
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        var id = await db.QuerySingleOrDefaultAsync<Guid>(
            sql,
            new { Provider = provider.ToString(), ProviderId = providerId }
        );

        return id;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username);
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(sql, new { Username = username });
    }
}
