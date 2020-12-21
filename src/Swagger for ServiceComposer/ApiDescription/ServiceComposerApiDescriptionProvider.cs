using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore.TypedViewModel;

namespace Swagger_for_ServiceComposer.ApiDescription
{
    internal class ServiceComposerApiDescriptionProvider : IApiDescriptionProvider
    {
        private static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();
        private readonly EndpointDataSource _endpointDataSource;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        
        public ServiceComposerApiDescriptionProvider(EndpointDataSource endpointDataSource, IModelMetadataProvider modelMetadataProvider)
        {
            _endpointDataSource = endpointDataSource;
            _modelMetadataProvider = modelMetadataProvider;
        }

        // Executes after ASP.NET Core
        public int Order => -900;

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            var endpoints = _endpointDataSource.Endpoints;

            foreach (var endpoint in endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var apiDescription = CreateApiDescription(routeEndpoint);

                    context.Results.Add(apiDescription);
                }
            }
        }

        private Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription CreateApiDescription(RouteEndpoint routeEndpoint)
        {
            var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            var verb = httpMethodMetadata?.HttpMethods.FirstOrDefault();

            var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription();
            // Default to a GET in case a Route map was registered inline - it's unlikely to be a composition handler in that case.
            apiDescription.HttpMethod = verb ?? "GET";
            apiDescription.ActionDescriptor = new ActionDescriptor
            {
                RouteValues = new Dictionary<string, string>
                {
                    // Swagger uses this to group endpoints together.
                    // Group methods together using the service name.
                    // NOTE: Need a metadata model in service composer to begin supplying more info other than just http verbs and route patterns.
                    ["controller"] =
                        "ViewModelComposition" // routeEndpoint.RoutePattern.GetParameter("controller").Default.ToString()
                }
            };
            apiDescription.RelativePath = routeEndpoint.RoutePattern.RawText.TrimStart('/');
            apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat {MediaType = "application/json"});

            foreach (var producesDefaultResponseTypeAttribute in routeEndpoint.Metadata
                .OfType<ProducesDefaultResponseTypeAttribute>())
            {
                apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                {
                    Type = producesDefaultResponseTypeAttribute.Type,
                    ApiResponseFormats = {new ApiResponseFormat {MediaType = "application/json"}},
                    StatusCode = producesDefaultResponseTypeAttribute.StatusCode,
                    IsDefaultResponse = true,
                    ModelMetadata = _modelMetadataProvider.GetMetadataForType(producesDefaultResponseTypeAttribute.Type)
                });
            }

            foreach (var producesResponseTypeAttribute in routeEndpoint.Metadata.OfType<ProducesResponseTypeAttribute>()
            )
            {
                apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                {
                    Type = producesResponseTypeAttribute.Type,
                    ApiResponseFormats = {new ApiResponseFormat {MediaType = "application/json"}},
                    StatusCode = producesResponseTypeAttribute.StatusCode,
                    IsDefaultResponse = false,
                    ModelMetadata = _modelMetadataProvider.GetMetadataForType(producesResponseTypeAttribute.Type)
                });
            }

            foreach (var apiParameterDescriptionAttribute in routeEndpoint.Metadata
                .OfType<ApiParameterDescriptionAttribute>())
            {
                apiDescription.ParameterDescriptions.Add(new ApiParameterDescription()
                {
                    Name = apiParameterDescriptionAttribute.Name,
                    IsRequired = apiParameterDescriptionAttribute.IsRequired,
                    Type = apiParameterDescriptionAttribute.Type,
                    Source = GetBindingSource(apiParameterDescriptionAttribute.Source),
                    ModelMetadata = _modelMetadataProvider.GetMetadataForType(apiParameterDescriptionAttribute.Type)
                });
            }

            var typedViewModelAttributes = routeEndpoint.Metadata
                .OfType<TypedViewModelAttribute>()
                .ToList();
            if (typedViewModelAttributes.Any())
            {
                var types = typedViewModelAttributes.Select(a => a.Type).Distinct();

                var ctor = typeof(DisplayNameAttribute).GetConstructor(new[] { typeof(string) });
                var options = new ProxyGenerationOptions()
                {
                    AdditionalAttributes =
                    {
                        new CustomAttributeInfo(ctor, new object[]{"composed view model"})
                    }
                };
                var vm = ProxyGenerator.CreateClassProxy(
                    typeof(ComposedViewModel), 
                    types.ToArray(),
                    options);
                var vmType = vm.GetType();

                apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                {
                    Type = vmType,
                    ApiResponseFormats = {new ApiResponseFormat {MediaType = "application/json"}},
                    StatusCode = 200,
                    IsDefaultResponse = true,
                    ModelMetadata = _modelMetadataProvider.GetMetadataForType(vmType)
                });
            }

            return apiDescription;
        }

        BindingSource GetBindingSource(string source)
        {
            var staticProps = typeof(BindingSource).GetFields(BindingFlags.Static | BindingFlags.Public);
            var prop = staticProps.Single(p => p.Name == source);

            return prop.GetValue(null) as BindingSource;
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            // no-op
        }
    }

    public abstract class ComposedViewModel
    {
        
    }
}