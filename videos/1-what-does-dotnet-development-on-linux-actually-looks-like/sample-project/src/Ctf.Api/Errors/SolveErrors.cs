namespace Ctf.Api.Errors;

public static class SolveErrors
{
    public static readonly Error AlreadySolved = new(
        StatusCodes.Status409Conflict,
        "You already solved this challenge."
    );

    public static readonly Error LoggedUserNotAPlayer = new(
        StatusCodes.Status403Forbidden,
        "You must be an player in this room to submit flags."
    );

    public static readonly Error MustHaveATeamToSolveChallenges = new(
        StatusCodes.Status400BadRequest,
        "You need to be in a team in order to solve challenges."
    );

    public static readonly Error NotAllowedToViewOtherTeamSolves = new(
        StatusCodes.Status403Forbidden,
        "Not allowed to view other team solves in this room."
    );

    public static readonly Error SubmissionsDisabled = new(
        StatusCodes.Status400BadRequest,
        "Submissions are disabled in this room."
    );
}
