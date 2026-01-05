using System.Security.Claims;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Users;
using FluentValidation;

namespace Ctf.Api.Features.RoomInvitations;

public static class InviteUserToRoom
{
    public sealed record InviteUserToRoomRequest(Guid RoomId, Guid InviteeId, RoomRole InviteeRole);

    public sealed record Command(Guid RoomId, Guid InviteeId, Guid InviterId, RoomRole InviteeRole);

    public sealed class Handler(
        IRoomInvitationRepository roomInvitationRepository,
        IUserRepository userRepository,
        IRoomMemberRepository roomMemberRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure(Error.Validation(validationResult.ToString()));

            var inviterRole = await roomMemberRepository.GetRoleAsync(
                request.RoomId,
                request.InviterId
            );

            if (inviterRole is null)
                return Result.Failure(RoomErrors.NotFound);

            if (inviterRole < RoomRole.Admin)
                return Result.Failure(RoomInvitationErrors.NotAllowed);

            if (request.InviteeRole >= inviterRole)
                return Result.Failure(RoomInvitationErrors.LowerRolesOnly);

            var inviteeExists = await userRepository.ExistsAsync(request.InviteeId);
            if (!inviteeExists)
                return Result.Failure(RoomInvitationErrors.InviteeNotFound);

            var inviteeAlreadyInivted = await roomInvitationRepository.ExistsAsync(
                request.RoomId,
                request.InviteeId
            );
            if (inviteeAlreadyInivted)
                return Result.Failure(RoomInvitationErrors.AlreadyInvited);

            var inviteeAlreadyMember = await roomMemberRepository.ExistsAsync(
                request.RoomId,
                request.InviteeId
            );
            if (inviteeAlreadyMember)
                return Result.Failure(RoomMemberErrors.AlreadyMember);

            var dto = new CreateRoomInvitationDto
            {
                RoomId = request.RoomId,
                InviteeId = request.InviteeId,
                InviterId = request.InviterId,
                InviteeRole = request.InviteeRole,
            };

            await roomInvitationRepository.CreateAsync(dto);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "invites",
                    async (
                        InviteUserToRoomRequest request,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(
                            request.RoomId,
                            request.InviteeId,
                            claims.GetLoggedInUserId(),
                            request.InviteeRole
                        );

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(RoomInvitations));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InviteeRole)
                .Must(role => Enum.IsDefined(role))
                .WithMessage(RoomInvitationConstants.InvalidRoleMessage);

            RuleFor(x => x)
                .Must(x => x.InviterId != x.InviteeId)
                .WithMessage(RoomInvitationConstants.CannotInviteSelfMessage);
        }
    }
}
