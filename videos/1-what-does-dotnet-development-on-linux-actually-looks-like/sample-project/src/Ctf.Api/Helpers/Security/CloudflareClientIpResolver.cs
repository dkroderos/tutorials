namespace Ctf.Api.Helpers.Security;

public sealed class CloudflareClientIpResolver : IClientIpResolver
{
    public string GetClientIp(HttpContext httpContext)
    {
        var ipAddress = httpContext.Request.Headers["Cf-Connecting-Ip"].ToString();

        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new InvalidOperationException(CommonConstants.MissingIpAddress);

        return ipAddress;
    }
}
