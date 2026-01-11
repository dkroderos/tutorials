using DbUp;

namespace Todo.Migrations;

public sealed class Worker(
    ILogger<Worker> logger,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting migrations...");

        var connectionString = configuration.GetConnectionString("todo");

        var upgrader = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(Worker).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (result.Successful)
            logger.LogInformation("Migration successful");
        else
            logger.LogError(result.Error, "Migration failed");

        lifetime.StopApplication();

        return Task.CompletedTask;
    }
}
