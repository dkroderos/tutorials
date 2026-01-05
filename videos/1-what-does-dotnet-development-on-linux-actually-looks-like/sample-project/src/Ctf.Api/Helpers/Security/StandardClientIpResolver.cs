namespace Ctf.Api.Helpers.Security;

public sealed class StandardClientIpResolver : IClientIpResolver
{
    public string GetClientIp(HttpContext httpContext)
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new InvalidOperationException(CommonConstants.MissingIpAddress);

        return ipAddress;
    }
}
