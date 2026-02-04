using Todo.Api.Extensions;

namespace Todo.Api.Features.Todos;

public static class MarkTodoAsComplete
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut(
                "todos/{id:guid}",
                async (Guid id, Repositories.Todos.ITodoRepository todoRepository) =>
                {
                    var marked = await todoRepository.MarkAsCompleteAsync(id);
                    if (!marked)
                        return Results.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            detail: "Incomplete todo not found."
                        );

                    return Results.NoContent();
                }
            );
        }
    }
}
