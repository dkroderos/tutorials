namespace Todo.Shared.Entities;

public sealed class TodoEntity
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool Completed { get; set; }
    public required DateTime CreatedAt { get; init; }
}
