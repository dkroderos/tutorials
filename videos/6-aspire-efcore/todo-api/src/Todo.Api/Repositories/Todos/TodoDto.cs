namespace Todo.Api.Repositories.Todos;

public sealed record TodoDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool Completed { get; init; }
    public required DateTime CreatedAt { get; init; }
}
