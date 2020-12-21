using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore;
using Swagger_for_ServiceComposer.ApiDescription;
using Swagger_for_ServiceComposer.Models;

namespace Swagger_for_ServiceComposer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            services.AddSwaggerGen();
            services.AddControllers(); // Swagger needs this for the ApiExplorer.
            services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, ServiceComposerApiDescriptionProvider>());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API"));

            app.UseRouting();
            app.UseEndpoints(builder => builder.MapCompositionHandlers());
        }
    }
}