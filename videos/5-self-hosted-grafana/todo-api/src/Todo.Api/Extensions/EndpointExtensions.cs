using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Todo.Api.Extensions;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

public static class EndpointExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEndpoints()
        {
            services.AddEndpoints(Assembly.GetExecutingAssembly());

            return services;
        }

        public IServiceCollection AddEndpoints(Assembly assembly)
        {
            var serviceDescriptors = assembly
                .DefinedTypes.Where(type =>
                    type is { IsAbstract: false, IsInterface: false }
                    && type.IsAssignableTo(typeof(IEndpoint))
                )
                .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
                .ToArray();

            services.TryAddEnumerable(serviceDescriptors);

            return services;
        }
    }

    extension(WebApplication app)
    {
        public IApplicationBuilder MapEndpoints(string prefix = "/api")
        {
            app.MapEndpoints(Assembly.GetExecutingAssembly(), prefix);

            return app;
        }

        public IApplicationBuilder MapEndpoints(Assembly assembly, string prefix)
        {
            var endpoints = app
                .Services.GetRequiredService<IEnumerable<IEndpoint>>()
                .Where(endpoint => endpoint.GetType().Assembly == assembly);

            var routeGroup = app.MapGroup(prefix);

            foreach (var endpoint in endpoints)
                endpoint.MapEndpoint(routeGroup);

            return app;
        }
    }
}
