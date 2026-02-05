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

var api = builder
    .AddProject<Projects.Todo_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WaitForCompletion(migrations);

var webUrl = builder.AddParameter("web-url");
var web = builder.AddExternalService("web", webUrl);

builder
    .AddProject<Projects.Todo_Gateway>("gateway")
    .WithReference(api)
    .WithReference(web)
    .WaitFor(api);

builder.Build().Run();
