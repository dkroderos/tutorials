namespace Ctf.Api.Constants;

public static class PasswordConstants
{
    public const string HasLowercase = @"[a-z]";
    public const string HasLowercaseMessage =
        "Password must contain at least one lowercase letter.";
    public const string HasUppercase = @"[A-Z]";
    public const string HasUppercaseMessage =
        "Password must contain at least one uppercase letter.";
    public const int MaxLength = 64;
    public static readonly string MaxLengthExceededMessage =
        $"Password must not exceed {MaxLength} characters.";
    public const int MinLength = 12;
    public static readonly string MinLengthNotMetMessage =
        $"Password must be at least {MinLength} characters long.";
    public const string RequiredMessage = "Password is required.";
}
