using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.OpenApi.ApiDescription;

namespace ServiceComposer.OpenApi.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceComposerOpenApiServices(this IServiceCollection services)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, ServiceComposerApiDescriptionProvider>());

            return services;
        }

    }
}
