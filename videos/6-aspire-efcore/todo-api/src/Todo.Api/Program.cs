using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Todo.Api.Exceptions;
using Todo.Api.Extensions;
using Todo.Api.Repositories.Todos;
using Todo.Shared.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("todo"))
);

builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddScoped<ITodoRepository, TodoRepository>();

builder.Services.AddEndpoints();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

app.UseExceptionHandler();

app.MapEndpoints();

app.Run();
