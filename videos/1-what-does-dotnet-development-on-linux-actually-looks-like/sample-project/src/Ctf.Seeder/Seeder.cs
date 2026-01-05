using System.Net;
using Ctf.Api.Helpers.Security;
using Npgsql;

namespace Ctf.Seeder;

public sealed class Seeder(
    NpgsqlDataSource dataSource,
    ILogger<Seeder> logger,
    IPasswordHasher passwordHasher,
    IHostApplicationLifetime hostApplicationLifetime
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting seed...");

        using var db = await dataSource.OpenConnectionAsync(stoppingToken);

        SeedUsers(db, passwordHasher, out var userIds);

        SeedRooms(db, userIds, out var roomIds);
        SeedChallenges(db, roomIds, [.. userIds.Take(roomIds.Count)]);

        logger.LogInformation("Seed succeeded...");

        hostApplicationLifetime.StopApplication();
    }

    private static void SeedUsers(
        NpgsqlConnection connection,
        IPasswordHasher passwordHasher,
        out List<Guid> userIds
    )
    {
        const int userCount = 1000;
        var now = DateTime.UtcNow;
        var users =
            new List<(
                Guid Id,
                string Username,
                string Email,
                string PasswordHash,
                DateTime CreatedAt,
                IPAddress RegistrationIp
            )>();

        userIds = [];

        var passwordHash = passwordHasher.Hash("Password123!");

        for (int i = 0; i < userCount; i++)
        {
            var userId = Guid.CreateVersion7();
            userIds.Add(userId);

            var username = $"user{i}";
            var email = $"user{i}@example.com";
            var registrationIp = IPAddress.Parse($"192.168.1.{i % 255}");

            users.Add((userId, username, email, passwordHash, now, registrationIp));
        }

        using var writer = connection.BeginBinaryImport(
            "COPY users (id, username, email, password_hash, is_verified, created_at, registration_ip) FROM STDIN (FORMAT BINARY)"
        );

        foreach (var (Id, Username, Email, PasswordHash, CreatedAt, RegistrationIp) in users)
        {
            writer.StartRow();
            writer.Write(Id, NpgsqlTypes.NpgsqlDbType.Uuid);
            writer.Write(Username, NpgsqlTypes.NpgsqlDbType.Varchar);
            writer.Write(Email, NpgsqlTypes.NpgsqlDbType.Varchar);
            writer.Write(PasswordHash, NpgsqlTypes.NpgsqlDbType.Varchar);
            writer.Write(true, NpgsqlTypes.NpgsqlDbType.Boolean);
            writer.Write(CreatedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz);
            writer.Write(RegistrationIp, NpgsqlTypes.NpgsqlDbType.Inet);
        }

        writer.Complete();
    }

    private static void SeedRooms(
        NpgsqlConnection connection,
        List<Guid> userIds,
        out List<Guid> roomIds
    )
    {
        roomIds = [];

        using (
            var roomWriter = connection.BeginBinaryImport(
                "COPY rooms (id, creator_id, name) FROM STDIN (FORMAT BINARY)"
            )
        )
        {
            for (int i = 0; i < 10; i++)
            {
                var roomId = Guid.CreateVersion7();
                roomIds.Add(roomId);

                roomWriter.StartRow();
                roomWriter.Write(roomId, NpgsqlTypes.NpgsqlDbType.Uuid);
                roomWriter.Write(userIds[i], NpgsqlTypes.NpgsqlDbType.Uuid);
                roomWriter.Write($"Room {i + 1}", NpgsqlTypes.NpgsqlDbType.Text);
            }

            roomWriter.Complete();
        }

        using var memberWriter = connection.BeginBinaryImport(
            "COPY room_members (room_id, user_id, room_role) FROM STDIN (FORMAT BINARY)"
        );

        for (int i = 0; i < 10; i++)
        {
            memberWriter.StartRow();
            memberWriter.Write(roomIds[i], NpgsqlTypes.NpgsqlDbType.Uuid);
            memberWriter.Write(userIds[i], NpgsqlTypes.NpgsqlDbType.Uuid);
            memberWriter.Write("owner", NpgsqlTypes.NpgsqlDbType.Text);
        }

        memberWriter.Complete();
    }

    private static void SeedChallenges(
        NpgsqlConnection connection,
        List<Guid> roomIds,
        List<Guid> userIds
    )
    {
        var challengeIds = new List<Guid>();
        using (
            var writer = connection.BeginBinaryImport(
                "COPY challenges (id, room_id, creator_id, name, description, max_attempts) FROM STDIN (FORMAT BINARY)"
            )
        )
        {
            for (int index = 0; index < roomIds.Count; index++)
            {
                var roomId = roomIds[index];
                for (int i = 0; i < 100; i++)
                {
                    var challengeId = Guid.CreateVersion7();
                    challengeIds.Add(challengeId);
                    writer.StartRow();
                    writer.Write(challengeId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    writer.Write(roomId, NpgsqlTypes.NpgsqlDbType.Uuid);
                    writer.Write(userIds[index], NpgsqlTypes.NpgsqlDbType.Uuid);
                    writer.Write($"Challenge {index * 100 + i + 1}", NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(
                        $"Challenge Description {index * 100 + i + 1}",
                        NpgsqlTypes.NpgsqlDbType.Text
                    );
                    writer.Write(0, NpgsqlTypes.NpgsqlDbType.Integer);
                }
            }
            writer.Complete();
        }

        using var flagWriter = connection.BeginBinaryImport(
            "COPY flags (challenge_id, value) FROM STDIN (FORMAT BINARY)"
        );
        foreach (var challengeId in challengeIds)
        {
            flagWriter.StartRow();
            flagWriter.Write(challengeId, NpgsqlTypes.NpgsqlDbType.Uuid);
            flagWriter.Write("flag", NpgsqlTypes.NpgsqlDbType.Varchar);
        }
        flagWriter.Complete();
    }
}
