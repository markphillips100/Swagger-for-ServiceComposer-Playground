﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using ServiceComposerAttributes;

namespace Swagger_for_ServiceComposer.ApiDescription
{
    internal class ServiceComposerApiDescriptionProvider : IApiDescriptionProvider
    {
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
                    ["controller"] = "ViewModelComposition"// routeEndpoint.RoutePattern.GetParameter("controller").Default.ToString()
                }
            };
            apiDescription.RelativePath = routeEndpoint.RoutePattern.RawText.TrimStart('/');
            apiDescription.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });

            foreach (var producesDefaultResponseTypeAttribute in routeEndpoint.Metadata.OfType<ProducesDefaultResponseTypeAttribute>())
            {
                apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                {
                    Type = producesDefaultResponseTypeAttribute.Type,
                    ApiResponseFormats = { new ApiResponseFormat { MediaType = "application/json" } },
                    StatusCode = producesDefaultResponseTypeAttribute.StatusCode,
                    IsDefaultResponse = true,
                    ModelMetadata = _modelMetadataProvider.GetMetadataForType(producesDefaultResponseTypeAttribute.Type)
                });
            }

            foreach (var producesResponseTypeAttribute in routeEndpoint.Metadata.OfType<ProducesResponseTypeAttribute>())
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

            foreach (var apiParameterDescriptionAttribute in routeEndpoint.Metadata.OfType<ApiParameterDescriptionAttribute>())
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

            return apiDescription;
        }

        BindingSource GetBindingSource(string source)
        {
            var staticProps = typeof(BindingSource).GetFields(BindingFlags.Static|BindingFlags.Public);
            var prop = staticProps.Single(p => p.Name == source);

            return prop.GetValue(null) as BindingSource;
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            // no-op
        }
    }
}