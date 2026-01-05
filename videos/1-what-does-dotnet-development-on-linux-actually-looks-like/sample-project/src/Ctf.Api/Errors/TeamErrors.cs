namespace Ctf.Api.Errors;

public static class TeamErrors
{
    public static readonly Error LoggedUserNotAnAdmin = new(
        StatusCodes.Status403Forbidden,
        "You must be an admin in this room to modify teams."
    );

    public static readonly Error NameAlreadyExists = new(
        StatusCodes.Status409Conflict,
        "There's already a team in the room with this name. Please choose a different name."
    );

    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "Team was not found."
    );
}
