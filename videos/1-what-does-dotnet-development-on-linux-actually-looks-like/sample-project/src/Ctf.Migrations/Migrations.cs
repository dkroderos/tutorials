using DbUp;
using Minio;
using Minio.DataModel.Args;

namespace Ctf.Migrations;

public sealed class Migrations(
    ILogger<Migrations> logger,
    IConfiguration configuration,
    IHostApplicationLifetime hostApplicationLifetime,
    IMinioClient minioClient
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting migrations...");

        var connectionString = configuration.GetConnectionString("db");

        var upgrader = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(Migrations).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            logger.LogError(result.Error, "Migration failed");
            hostApplicationLifetime.StopApplication();
            return;
        }

        logger.LogInformation("Migration successful");

        var bucketName = "artifacts";
        bool exists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName),
            cancellationToken: stoppingToken
        );

        if (!exists)
        {
            logger.LogInformation("Creating bucket {BucketName} in MinIO", bucketName);
            await minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(bucketName),
                cancellationToken: stoppingToken
            );
        }
        else
            logger.LogInformation("Bucket {BucketName} already exists in MinIO", bucketName);

        hostApplicationLifetime.StopApplication();
    }
}
