namespace Ctf.Api.Constants;

public static class ChallengeConstants
{
    public const int DescriptionMaxLength = 2000;
    public static readonly string DescriptionMaxLengthExceededMessage =
        $"Challenge description must not exceed {DescriptionMaxLength} characters.";
    public const string DescriptionRequiredMessage = "Challenge description is required.";
    public const string MaxAttemptsLessThanZeroMessage =
        "Max attempts must be greater than or equal to 0.";
    public const int NameMaxLength = 64;
    public static readonly string NameMaxLengthExceededMessage =
        $"Challenge name must not exceed {NameMaxLength} characters.";
    public const string NameRequiredMessage = "Challenge name is required.";
}
