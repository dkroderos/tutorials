using Ctf.Api.Helpers.Security;

namespace Ctf.Api.Helpers;

public static class DependencyInjection
{
    public static IServiceCollection AddHelpers(this IServiceCollection services)
    {
        services.AddScoped<IAuthHelper, AuthHelper>();
        // builder.Services.AddSingleton<IClientIpResolver, CloudflareClientIpResolver>();
        services.AddSingleton<IClientIpResolver, StandardClientIpResolver>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        return services;
    }
}
