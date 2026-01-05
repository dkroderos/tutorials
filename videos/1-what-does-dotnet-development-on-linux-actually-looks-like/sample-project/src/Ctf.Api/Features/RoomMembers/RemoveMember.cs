using System.Security.Claims;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using FluentValidation;

namespace Ctf.Api.Features.RoomMembers;

public static class RemoveMember
{
    public sealed record Command(Guid RoomId, Guid TargetUserId, Guid RemoverId);

    public sealed class Handler(
        IRoomMemberRepository roomMemberRepository,
        IValidator<Command> validator
    ) : IFeature
    {
        public async Task<Result> Handle(Command request)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Result.Failure(Error.Validation(validationResult.ToString()));

            var removerRole = await roomMemberRepository.GetRoleAsync(
                request.RoomId,
                request.RemoverId
            );

            if (removerRole is null)
                return Result.Failure(RoomErrors.NotFound);

            if (removerRole < RoomRole.Admin)
                return Result.Failure(RoomMemberErrors.LoggedUserMustBeAdminToRemoveMembers);

            var targetUserRole = await roomMemberRepository.GetRoleAsync(
                request.RoomId,
                request.TargetUserId
            );

            if (targetUserRole is null)
                return Result.Failure(UserErrors.NotFound);

            if (targetUserRole >= removerRole)
                return Result.Failure(RoomMemberErrors.RemoveLowerRolesOnly);

            var deleted = await roomMemberRepository.DeleteAsync(
                request.RoomId,
                request.TargetUserId
            );

            if (!deleted)
                return Result.Failure(UserErrors.NotFound);

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "rooms/{roomId:guid}/members/{targetUserId:guid}/remove",
                    async (
                        Guid roomId,
                        Guid targetUserId,
                        Handler handler,
                        ClaimsPrincipal claims
                    ) =>
                    {
                        var command = new Command(roomId, targetUserId, claims.GetLoggedInUserId());

                        var result = await handler.Handle(command);
                        return result.ToHttpResult();
                    }
                )
                .RequireAuthorization()
                .WithTags(nameof(RoomMembers));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x)
                .Must(x => x.TargetUserId != x.RemoverId)
                .WithMessage(RoomMemberConstants.CannotSelfRemoveMessage);
        }
    }
}
