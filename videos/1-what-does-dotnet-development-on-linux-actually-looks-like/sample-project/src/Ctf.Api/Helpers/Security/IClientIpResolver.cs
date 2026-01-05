namespace Ctf.Api.Helpers.Security;

public interface IClientIpResolver
{
    string GetClientIp(HttpContext httpContext);
}
