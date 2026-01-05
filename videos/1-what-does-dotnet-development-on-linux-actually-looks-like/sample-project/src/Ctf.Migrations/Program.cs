using Ctf.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddMinioClient("minio");

builder.Services.AddHostedService<Migrations>();

builder.Build().Run();
