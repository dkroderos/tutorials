namespace Ctf.Api.Constants;

public static class UserConstants
{
    public const string UsernameRequiredMessage = "Username is required.";
    public const int UsernameMaxLength = 128;
    public static readonly string UsernameMaxLengthExceededMessage =
        $"Username must not exceed {UsernameMaxLength} characters.";
}
