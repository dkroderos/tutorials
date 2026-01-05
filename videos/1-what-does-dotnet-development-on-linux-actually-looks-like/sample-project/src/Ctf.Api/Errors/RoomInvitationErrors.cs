namespace Ctf.Api.Errors;

public static class RoomInvitationErrors
{
    public static readonly Error AlreadyInvited = new(
        StatusCodes.Status409Conflict,
        "The user is already a invited in the room."
    );
    public static readonly Error LowerRolesOnly = new(
        StatusCodes.Status403Forbidden,
        "You can only invite users with a lower role than yours."
    );
    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "Invitation was not found."
    );
    public static readonly Error InviteeNotFound = new(
        StatusCodes.Status404NotFound,
        "The user to invite was not found."
    );
    public static readonly Error NotAllowed = new(
        StatusCodes.Status403Forbidden,
        "Not allowed to invite users in the specified room."
    );
}
