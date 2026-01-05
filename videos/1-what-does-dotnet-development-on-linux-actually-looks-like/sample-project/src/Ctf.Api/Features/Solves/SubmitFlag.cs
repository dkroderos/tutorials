using System.Security.Claims;
using Ctf.Api.Repositories.Challenges;
using Ctf.Api.Repositories.Flags;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Solves;
using Ctf.Api.Repositories.TeamMembers;
using FluentValidation;

namespace Ctf.Api.Features.Solves;

public static class SubmitFlag
{
    public sealed record SubmitFlagRequest(Guid ChallengeId, string Flag);

    public sealed record Command(Guid UserId, Guid ChallengeId, string Flag);

    public sealed record Response(FlagStatus Status);

    public enum FlagStatus
    {
        Incorrect,
        Correct,
    }

    public sealed class Handler(
        IChallengeRepository challengeRepository,
        IRoomMemberRepository roomMemberRepository,
        IRoomRepository roomRepository,
        ITeamMemberRepository teamMemberRepository,
        IFlagRepository flagRepository,
        ISolveRepository solveRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Command request)
        {
            var now = DateTime.UtcNow;

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<Response>(Error.Validation(validationResult.ToString()));

            var roomId = await challengeRepository.GetRoomIdAsync(request.ChallengeId);
            if (roomId is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            var role = await roomMemberRepository.GetRoleAsync(roomId.Value, request.UserId);
            if (role is null)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            if (role != RoomRole.Player)
                return Result.Failure<Response>(SolveErrors.LoggedUserNotAPlayer);

            var roomSolveRequirementsDto = await roomRepository.GetRoomSolveRequirementsAsync(
                roomId.Value
            );

            if (roomSolveRequirementsDto is null || roomSolveRequirementsDto.AreChallengesHidden)
                return Result.Failure<Response>(ChallengeErrors.NotFound);

            if (
                roomSolveRequirementsDto.IsSubmissionsForceDisabled
                || now < roomSolveRequirementsDto.SubmissionStart
                || now > roomSolveRequirementsDto.SubmissionEnd
            )
                return Result.Failure<Response>(SolveErrors.SubmissionsDisabled);

            var playerTeamId = await teamMemberRepository.GetUserTeamAsync(
                roomId.Value,
                request.UserId
            );

            if (playerTeamId is null)
                return Result.Failure<Response>(SolveErrors.MustHaveATeamToSolveChallenges);

            var alreadySolved = await solveRepository.ExistsAsync(
                request.UserId,
                playerTeamId.Value
            );
            if (alreadySolved)
                return Result.Failure<Response>(SolveErrors.AlreadySolved);

            var flags = await flagRepository.GetByChallengeIdAsync(request.ChallengeId);
            if (!flags.Contains(request.Flag))
                return new Response(FlagStatus.Incorrect);

            var createSolveDto = new CreateSolveDto
            {
                ChallengeId = request.ChallengeId,
                TeamId = playerTeamId.Value,
            };

            await solveRepository.CreateAsync(createSolveDto);

            var response = new Response(FlagStatus.Correct);
            return response;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "challenges/{challengeid:guid}/solves",
                    async (
                        Guid challengeId,
                        SubmitFlagRequest request,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(
                            claims.GetLoggedInUserId(),
                            challengeId,
                            request.Flag
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Solves));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Flag)
                .NotEmpty()
                .WithMessage(SolveConstants.FlagRequiredMessage)
                .MaximumLength(SolveConstants.FlagMaxLength)
                .WithMessage(SolveConstants.FlagMaxLengthExceededMessage);
        }
    }
}
