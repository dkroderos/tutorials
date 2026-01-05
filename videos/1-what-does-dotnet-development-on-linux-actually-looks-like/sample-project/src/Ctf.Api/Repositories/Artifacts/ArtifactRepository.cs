using Dapper;
using Minio;
using Minio.DataModel.Args;
using Npgsql;

namespace Ctf.Api.Repositories.Artifacts;

public sealed class ArtifactRepository(NpgsqlDataSource dataSource, IMinioClient minioClient)
    : IArtifactRepository
{
    private const string BucketName = "artifacts";

    public async Task AddAsync(AddArtifactDto dto)
    {
        const string sql = """
            INSERT INTO artifacts (challenge_id, file_name, file_size, uploader_id, content_type)
            VALUES (@ChallengeId, @FileName, @FileSize, @UploaderId, @ContentType)
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        await db.ExecuteAsync(
            sql,
            new
            {
                dto.ChallengeId,
                dto.FileName,
                dto.FileSize,
                dto.UploaderId,
                dto.ContentType,
            },
            transaction: tx
        );

        await minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(GetObjectName(dto.ChallengeId, dto.FileName))
                .WithStreamData(dto.Data)
                .WithObjectSize(dto.FileSize)
                .WithContentType(dto.ContentType)
        );

        await tx.CommitAsync();
    }

    public async Task<bool> DeleteAsync(Guid challengeId, string fileName)
    {
        const string sql = """
            DELETE FROM artifacts
            WHERE challenge_id = @ChallengeId AND file_name = @FileName
            """;

        var objectName = GetObjectName(challengeId, fileName);

        await using var db = await dataSource.OpenConnectionAsync();
        await using var tx = await db.BeginTransactionAsync();

        var rowsAffected = await db.ExecuteAsync(
            sql,
            new { ChallengeId = challengeId, FileName = fileName },
            transaction: tx
        );

        if (rowsAffected == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        await minioClient.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(BucketName).WithObject(objectName)
        );

        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid challengeId, string fileName)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM artifacts
                WHERE challenge_id = @ChallengeId AND file_name = @FileName
            );
            """;

        await using var db = await dataSource.OpenConnectionAsync();
        return await db.ExecuteScalarAsync<bool>(
            sql,
            new { ChallengeId = challengeId, FileName = fileName }
        );
    }

    private static string GetObjectName(Guid challengeId, string fileName) =>
        $"{challengeId}/{Uri.EscapeDataString(fileName)}";

    public async Task<Stream?> GetStreamAsync(Guid challengeId, string fileName)
    {
        var objectName = GetObjectName(challengeId, fileName);
        var ms = new MemoryStream();

        try
        {
            await minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(stream => stream.CopyTo(ms))
            );
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            const string sql = """
                DELETE FROM artifacts 
                WHERE challenge_id = @ChallengeId AND file_name = @FileName
                """;

            await using var db = await dataSource.OpenConnectionAsync();
            await db.ExecuteAsync(sql, new { ChallengeId = challengeId, FileName = fileName });

            return null;
        }

        ms.Position = 0;
        return ms;
    }
}
