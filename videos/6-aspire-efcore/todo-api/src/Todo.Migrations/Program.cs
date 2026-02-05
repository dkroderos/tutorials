using Microsoft.EntityFrameworkCore;
using Todo.Migrations;
using Todo.Shared.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("todo"))
);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
