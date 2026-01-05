namespace Ctf.Api.Common;

public sealed record Error(int Code, string Detail)
{
    public static readonly Error None = new(StatusCodes.Status418ImATeapot, string.Empty);

    public static Error Validation(string detail) => new(StatusCodes.Status400BadRequest, detail);
}
