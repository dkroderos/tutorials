namespace Ctf.Api.Constants;

public static class TeamConstants
{
    public const int NameMaxLength = 64;
    public static readonly string NameMaxLengthExceededMessage =
        $"Team name must not exceed {NameMaxLength} characters.";
    public const string NameRequiredMessage = "Team name is required.";
}
