var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session);

var db = postgres.AddDatabase("todo");

builder
    .AddProject<Projects.Todo_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db)
    .WithExplicitStart();

builder.Build().Run();
