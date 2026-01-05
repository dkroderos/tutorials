namespace Ctf.Api.Errors;

public static class UserErrors
{
    public static readonly Error EmailNotVerified = new(
        StatusCodes.Status403Forbidden,
        "Email is not verified. Please verify your email."
    );
    public static readonly Error EmailTaken = new(
        StatusCodes.Status409Conflict,
        "Email is already taken."
    );
    public static readonly Error IncorrectEmailOrPassword = new(
        StatusCodes.Status400BadRequest,
        "Incorrect email or password."
    );
    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "User was not found."
    );
    public static readonly Error InvalidAccess = new(
        StatusCodes.Status401Unauthorized,
        "Invalid access."
    );
    public static readonly Error UsernameTaken = new(
        StatusCodes.Status409Conflict,
        "Username is already taken."
    );
}
