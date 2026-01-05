var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Session);
var db = postgres.AddDatabase("db");

builder.AddProject<Projects.Ctf_Seeder>("seeder").WithReference(db).WithExplicitStart();

var valkey = builder.AddValkey("valkey").WithDataVolume().WithLifetime(ContainerLifetime.Session);
var minio = builder
    .AddMinioContainer("minio")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Session);

builder
    .AddProject<Projects.Ctf_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(minio)
    .WaitFor(minio)
    .WithExplicitStart();

var api = builder
    .AddProject<Projects.Ctf_Api>("api")
    .WithReference(db)
    .WithReference(valkey)
    .WithReference(minio);
builder.AddProject<Projects.Ctf_Proxy>("proxy").WithReference(api);

builder.Build().Run();
