namespace Ctf.Api.Constants;

public static class FlagConstants
{
    public const int MaxCount = 10;
    public static readonly string MaxCountExceededMessage =
        $"A challenge can have at most {MaxCount} flags.";
    public const int MaxLength = 500;
    public static readonly string MaxLengthExceededMessage =
        $"Each flag must be {MaxLength} characters or less.";
    public const string MustBeNonEmptyMessage = "All flags must be non-empty.";
    public const string RequiredMessage = "Flags are required.";
}
