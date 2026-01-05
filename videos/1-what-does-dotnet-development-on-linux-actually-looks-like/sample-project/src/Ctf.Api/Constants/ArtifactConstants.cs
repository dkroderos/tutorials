namespace Ctf.Api.Constants;

public static class ArtifactConstants
{
    public const int ContentTypeMaxLength = 100;
    public static readonly string ContentTypeMaxLengthExceededMessage =
        $"Content Type should not exceed {ContentTypeMaxLength} characters.";
    public const string ContentTypeRequiredMessage = "Content Type is required.";
    public const string FileNameContainsInvalidCharacters =
        "File name contains invalid characters.";
    public const int FileNameMaxLength = 100;
    public static readonly string FileNameMaxLengthExceededMessge =
        $"File name should not exceed {FileNameMaxLength} characters.";
    public const string FileNameRequiredMessage = "File name is required.";
    public static readonly string MaxFileSizeExceededMessage =
        $"File size must not exceed {MaxFileSizeInMegabytes} MB.";
    public const int MaxFileSizeInBytes = MaxFileSizeInMegabytes * 1024 * 1024;
    public const int MaxFileSizeInMegabytes = 2;
}
