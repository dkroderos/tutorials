namespace Ctf.Api.Errors;

public static class RoomErrors
{
    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "Room was not found."
    );
    public static readonly Error NameAlreadyExists = new(
        StatusCodes.Status409Conflict,
        "You already have a room with this name. Please choose a different name."
    );
    public static readonly Error PlayerCreatedTeamsNotAllowed = new(
        StatusCodes.Status403Forbidden,
        "Player created teams are not allowed in this room."
    );
}
