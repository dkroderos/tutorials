namespace Ctf.Api.Errors;

public static class RoomMemberErrors
{
    public static readonly Error AlreadyMember = new(
        StatusCodes.Status409Conflict,
        "The user is already a member of the room."
    );
    public static readonly Error LoggedUserAlreadyMember = new(
        StatusCodes.Status409Conflict,
        "You are already a member of the room."
    );
    public static readonly Error LoggedUserMustBeAdminToRemoveMembers = new(
        StatusCodes.Status409Conflict,
        "You must be an admin in this room to remove members."
    );
    public static readonly Error NotAMember = new(
        StatusCodes.Status404NotFound,
        "The user is not a member in the room."
    );
    public static readonly Error OwnerCannotLeave = new(
        StatusCodes.Status400BadRequest,
        "The owner cannot leave the room."
    );
    public static readonly Error RemoveLowerRolesOnly = new(
        StatusCodes.Status403Forbidden,
        "You can only remove users that are in lower roles than yours."
    );
}
