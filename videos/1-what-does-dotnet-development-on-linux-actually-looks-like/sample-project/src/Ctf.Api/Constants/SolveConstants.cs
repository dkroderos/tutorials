namespace Ctf.Api.Constants;

public static class SolveConstants
{
    public const string FlagRequiredMessage = "Flag is required.";
    public const int FlagMaxLength = 500;
    public static readonly string FlagMaxLengthExceededMessage =
        $"Flag should not exceed {FlagMaxLength} characters.";
}
