using System.Security.Claims;
using Ctf.Api.Repositories.Artifacts;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using FluentValidation;

namespace Ctf.Api.Features.Artifacts;

public static class AddArtifact
{
    public sealed record Command(
        Guid UploaderId,
        Guid ChallengeId,
        string FileName,
        long FileSize,
        string ContentType,
        Stream Data
    );

    public sealed class Handler(
        IChallengeRepository challengeRepository,
        IRoomMemberRepository roomMemberRepository,
        IArtifactRepository artifactRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure(Error.Validation(validationResult.ToString()));

            var roomId = await challengeRepository.GetRoomIdAsync(request.ChallengeId);
            if (roomId is null)
                return Result.Failure(ChallengeErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UploaderId);
            if (role is null)
                return Result.Failure(ChallengeErrors.NotFound);

            if (role < RoomRole.Editor)
                return Result.Failure(ChallengeErrors.LoggedUserNotAnEditor);

            var alreadyExists = await artifactRepository.ExistsAsync(
                request.ChallengeId,
                request.FileName
            );
            if (alreadyExists)
                return Result.Failure(ArtifactErrors.FileNameAlreadyExists);

            var dto = new AddArtifactDto
            {
                ChallengeId = request.ChallengeId,
                UploaderId = request.UploaderId,
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                Data = request.Data,
            };

            await artifactRepository.AddAsync(dto);
            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "challenges/{challengeid:guid}/artifacts",
                    async (
                        Guid challengeId,
                        IFormFile file,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        await using var stream = file.OpenReadStream();

                        var command = new Command(
                            claims.GetLoggedInUserId(),
                            challengeId,
                            file.FileName,
                            file.Length,
                            file.ContentType,
                            stream
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .DisableAntiforgery()
                .WithTags(nameof(Artifacts));
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.FileName)
                .NotEmpty()
                .WithMessage(ArtifactConstants.FileNameRequiredMessage)
                .MaximumLength(ArtifactConstants.FileNameMaxLength)
                .WithMessage(ArtifactConstants.FileNameMaxLengthExceededMessge)
                .Must(BeAValidFileName)
                .WithMessage(ArtifactConstants.FileNameContainsInvalidCharacters);

            RuleFor(c => c.FileSize)
                .LessThanOrEqualTo(ArtifactConstants.MaxFileSizeInBytes)
                .WithMessage(ArtifactConstants.MaxFileSizeExceededMessage);

            RuleFor(c => c.ContentType)
                .NotEmpty()
                .WithMessage(ArtifactConstants.ContentTypeRequiredMessage)
                .MaximumLength(ArtifactConstants.ContentTypeMaxLength)
                .WithMessage(ArtifactConstants.ContentTypeMaxLengthExceededMessage);
        }

        private static bool BeAValidFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return !fileName.Any(ch => invalidChars.Contains(ch));
        }
    }
}
