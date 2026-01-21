var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session);

var db = postgres.AddDatabase("todo");

var migrations = builder
    .AddProject<Projects.Todo_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db);

builder
    .AddProject<Projects.Todo_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WaitForCompletion(migrations);

builder.Build().Run();
