namespace Ctf.Api.Errors;

public static class ChallengeErrors
{
    public static readonly Error LoggedUserNotAnEditor = new(
        StatusCodes.Status403Forbidden,
        "You must be an editor in this room to modify challenges."
    );
    public static readonly Error NameAlreadyExists = new(
        StatusCodes.Status409Conflict,
        "There's already a challenge in the room with this name. Please choose a different name."
    );
    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "Challenge was not found."
    );
}
