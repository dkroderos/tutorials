using Ctf.Api.Helpers.Security;
using Ctf.Seeder;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("db");

builder.Services.AddHostedService<Seeder>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Build().Run();
