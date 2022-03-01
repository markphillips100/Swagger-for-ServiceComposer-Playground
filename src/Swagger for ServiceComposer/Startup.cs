using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;
using ServiceComposer.OpenApi.Configuration;

namespace Swagger_for_ServiceComposer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddViewModelComposition();

            services.AddSwaggerGen();
            services.AddControllers(); // Swagger needs this for the ApiExplorer.
            services.AddServiceComposerOpenApiServices();
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