namespace Ctf.Api.Errors;

public static class TeamMemberErrors
{
    public static readonly Error AlreadyHasTeam = new(
        StatusCodes.Status409Conflict,
        "The player is already has a team."
    );

    public static readonly Error CandidateMustBeAPlayer = new(
        StatusCodes.Status403Forbidden,
        "The candidate must have a player role in order to be added in a team."
    );

    public static readonly Error UserNotInTheTeam = new(
        StatusCodes.Status404NotFound,
        "The user is not in the team."
    );

    public static readonly Error LoggedUserNotInTheTeam = new(
        StatusCodes.Status404NotFound,
        "Please join in a team first."
    );
}
