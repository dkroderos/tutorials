using System.Reflection;

namespace Ctf.Api.Extensions;

public interface IFeature;

public static class FeatureExtensions
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddFeatures(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddFeatures(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        var serviceDescriptors = assembly.DefinedTypes.Where(type =>
            type is { IsAbstract: false, IsInterface: false }
            && typeof(IFeature).IsAssignableFrom(type)
        );

        foreach (var type in serviceDescriptors)
        {
            services.AddScoped(type);
        }

        return services;
    }
}
