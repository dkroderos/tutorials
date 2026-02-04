using Todo.Api.Extensions;
using Todo.Api.Repositories.Todos;

namespace Todo.Api.Features.Todos;

public static class DeleteTodo
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                "todos/{id:guid}",
                async (Guid id, ITodoRepository todoRepository) =>
                {
                    var deleted = await todoRepository.DeleteAsync(id);
                    if (!deleted)
                        return Results.Problem(
                            statusCode: StatusCodes.Status404NotFound,
                            detail: "Todo not found."
                        );

                    return Results.NoContent();
                }
            );
        }
    }
}
