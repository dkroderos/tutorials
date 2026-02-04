using Microsoft.EntityFrameworkCore;
using Todo.Shared.Data;
using Todo.Shared.Entities;

namespace Todo.Api.Repositories.Todos;

public sealed class TodoRepository(AppDbContext context) : ITodoRepository
{
    public async Task<TodoDto> CreateAsync(CreateTodoDto dto)
    {
        var id = Guid.CreateVersion7();
        var entity = new TodoEntity
        {
            Id = id,
            Name = dto.Name,
            Completed = false,
            CreatedAt = DateTime.UtcNow,
        };

        context.Todos.Add(entity);

        await context.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await context.Todos.FirstOrDefaultAsync(entity => entity.Id == id);

        if (entity is null)
            return false;

        context.Todos.Remove(entity);

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await context.Todos.AnyAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<TodoDto>> GetAllAsync()
    {
        return await context.Todos.Select(entity => ToDto(entity)).ToListAsync();
    }

    public async Task<bool> MarkAsCompleteAsync(Guid id)
    {
        var entity = await context.Todos.FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
            return false;

        entity.Completed = true;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        return await context.Todos.AnyAsync(x => x.Name == name);
    }

    private static TodoDto ToDto(TodoEntity entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Completed = entity.Completed,
            CreatedAt = entity.CreatedAt,
        };
}
