using System.ComponentModel.DataAnnotations;

namespace Ctf.Api.Options;

public sealed class SmtpOptions
{
    [Required]
    public required string SenderAddress { get; init; }

    [Required]
    public required string SenderPassword { get; init; }
}
