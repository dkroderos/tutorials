using Dapper;
using Npgsql;

namespace Todo.Api.Repositories.Todos;

public sealed class TodoRepository(NpgsqlDataSource source) : ITodoRepository
{
    public async Task<TodoDto> CreateAsync(CreateTodoDto dto)
    {
        const string sql = """
            INSERT INTO todos (id, name)
            VALUES (@Id, @Name)
            RETURNING id as Id, name as Name, completed as Completed, created_at as CreatedAt
            """;

        await using var db = await source.OpenConnectionAsync();

        var id = Guid.CreateVersion7();

        var todo = await db.QuerySingleAsync<TodoDto>(sql, new { Id = id, dto.Name });

        return todo;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = """
            DELETE FROM todos
            WHERE id = @Id
            """;

        await using var db = await source.OpenConnectionAsync();

        var rows = await db.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM todos
                WHERE id = @Id
            )
            """;

        await using var db = await source.OpenConnectionAsync();

        var exists = await db.ExecuteScalarAsync<bool>(sql, new { Id = id });
        return exists;
    }

    public async Task<IEnumerable<TodoDto>> GetAllAsync()
    {
        const string sql = """
            SELECT id as Id, name as Name, completed as Completed, created_at as CreatedAt
            FROM todos
            ORDER BY completed, created_at DESC
            """;

        await using var db = await source.OpenConnectionAsync();

        var dtos = await db.QueryAsync<TodoDto>(sql);

        return dtos;
    }

    public async Task<bool> MarkAsCompleteAsync(Guid id)
    {
        const string sql = """
            UPDATE todos
            SET completed = true
            WHERE id = @Id AND completed = false
            """;

        await using var db = await source.OpenConnectionAsync();

        var rows = await db.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM todos
                WHERE name = @Name
            )
            """;

        await using var db = await source.OpenConnectionAsync();

        var exists = await db.ExecuteScalarAsync<bool>(sql, new { Name = name });
        return exists;
    }
}
