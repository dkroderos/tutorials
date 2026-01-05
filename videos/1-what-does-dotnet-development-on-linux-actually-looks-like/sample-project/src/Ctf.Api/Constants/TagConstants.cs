namespace Ctf.Api.Constants;

public static class TagConstants
{
    public const int MaxCount = 10;
    public static readonly string MaxCountExceededMessage =
        $"A challenge can have at most {MaxCount} tags.";
    public const string MustBeNonEmptyMessage = "All tags must be non-empty.";
    public const int MaxLength = 20;
    public static readonly string MaxLengthExceededMessage =
        $"Each tag must be {MaxLength} characters or less.";
}
