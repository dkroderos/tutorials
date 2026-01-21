using Todo.Api.Extensions;
using Todo.Api.Repositories.Todos;

namespace Todo.Api.Features.Todos;

public static class GetTodos
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet(
                "/todos",
                async (ITodoRepository todoRepository) =>
                {
                    var dtos = await todoRepository.GetAllAsync();

                    var responses = dtos.Select(todo => new Response
                    {
                        Id = todo.Id,
                        Name = todo.Name,
                        Completed = todo.Completed,
                        CreatedAt = todo.CreatedAt,
                    });

                    return Results.Ok(responses);
                }
            );
        }
    }

    public sealed record Response
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required bool Completed { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
