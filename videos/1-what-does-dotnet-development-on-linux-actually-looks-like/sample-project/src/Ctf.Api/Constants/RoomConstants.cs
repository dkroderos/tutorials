namespace Ctf.Api.Constants;

public sealed class RoomConstants
{
    public const int DescriptionMaxLength = 2000;
    public static readonly string DescriptionMaxLengthExceededMessage =
        $"Room description must not exceed {DescriptionMaxLength} characters.";
    public const int NameMaxLength = 64;
    public static readonly string NameMaxLengthExceededMessage =
        $"Room name must not exceed {NameMaxLength} characters.";
    public const string NameRequiredMessage = "Room name is required.";
}
