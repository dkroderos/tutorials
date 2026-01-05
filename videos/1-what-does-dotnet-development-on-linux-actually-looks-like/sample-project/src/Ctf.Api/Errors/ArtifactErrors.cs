namespace Ctf.Api.Errors;

public static class ArtifactErrors
{
    public static readonly Error FileNameAlreadyExists = new(
        StatusCodes.Status409Conflict,
        "The file name already exists in the challenge."
    );
    public static readonly Error NotFound = new(
        StatusCodes.Status404NotFound,
        "Artifact was not found."
    );
}
