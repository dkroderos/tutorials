namespace Todo.Api.Repositories.Todos;

public interface ITodoRepository
{
    Task<TodoDto> CreateAsync(CreateTodoDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<TodoDto>> GetAllAsync();
    Task<bool> MarkAsCompleteAsync(Guid id);
    Task<bool> NameExistsAsync(string name);
}
