namespace Todo.Api.Repositories.Todos;

public sealed record CreateTodoDto
{
    public required string Name { get; init; }
}
