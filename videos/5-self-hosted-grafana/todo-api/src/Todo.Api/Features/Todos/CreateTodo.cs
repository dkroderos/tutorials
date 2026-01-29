using Todo.Api.Extensions;
using Todo.Api.Repositories.Todos;

namespace Todo.Api.Features.Todos;

public static class CreateTodo
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                "/todos",
                async (Request request, ITodoRepository todoRepository) =>
                {
                    if (request.Name == "throw")
                        throw new Exception("Simulated exception");

                    var exists = await todoRepository.NameExistsAsync(request.Name);

                    if (exists)
                        return Results.Problem(
                            statusCode: StatusCodes.Status409Conflict,
                            detail: "A todo with the same name already exists."
                        );

                    if (string.IsNullOrWhiteSpace(request.Name))
                        return Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            detail: "Name is required."
                        );

                    var dto = await todoRepository.CreateAsync(
                        new CreateTodoDto { Name = request.Name }
                    );

                    var response = new Response
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        Completed = dto.Completed,
                        CreatedAt = dto.CreatedAt,
                    };

                    return Results.Ok(response);
                }
            );
        }
    }

    public sealed record Request
    {
        public required string Name { get; init; }
    }

    public sealed record Response
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required bool Completed { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
