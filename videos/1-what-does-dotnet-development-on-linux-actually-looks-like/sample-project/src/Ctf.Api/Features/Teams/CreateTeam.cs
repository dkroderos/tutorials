using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;
using FluentValidation;

namespace Ctf.Api.Features.Teams;

public static class CreateTeam
{
    public sealed record CreateTeamRequest(Guid RoomId, string Name);

    public sealed record Command(Guid RoomId, Guid UserId, string Name);

    public sealed record Response(Guid Id);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        ITeamRepository teamRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result<Response>> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure<Response>(Error.Validation(validationResult.ToString()));

            var role = await roomMemberRepository.GetRoleAsync(request.RoomId, request.UserId);
            if (role is null)
                return Result.Failure<Response>(RoomErrors.NotFound);

            if (role < RoomRole.Admin)
                return Result.Failure<Response>(TeamErrors.LoggedUserNotAnAdmin);

            var nameInRoomExists = await teamRepository.NameInRoomExistsAsync(
                request.RoomId,
                request.Name
            );

            if (nameInRoomExists)
                return Result.Failure<Response>(TeamErrors.NameAlreadyExists);

            var dto = new CreateTeamDto { RoomId = request.RoomId, Name = request.Name };
            var id = await teamRepository.CreateAsync(dto);

            return new Response(id);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "teams",
                    async (CreateTeamRequest request, Handler handler, ClaimsPrincipal claims) =>
                    {
                        var command = new Command(
                            request.RoomId,
                            claims.GetLoggedInUserId(),
                            request.Name
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(Teams));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name)
                .NotEmpty()
                .WithMessage(TeamConstants.NameRequiredMessage)
                .MaximumLength(TeamConstants.NameMaxLength)
                .WithMessage(TeamConstants.NameMaxLengthExceededMessage);
        }
    }
}
