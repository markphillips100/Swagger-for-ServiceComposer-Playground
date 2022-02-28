﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;
using ServiceComposerAttributes;
using Swagger_for_ServiceComposer.ApiDescription;

namespace Swagger_for_ServiceComposer.Handlers.ServiceA
{
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        [ProducesDefaultResponseType(typeof(void))]
        [ProducesCompositionResponseType("AValue", typeof(int))]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ApiParameterDescription(Name = "id", IsRequired = true, Type = typeof(int), Source = "Path")]
        public Task Handle(HttpRequest request)
        {
            var routeData = request.HttpContext.GetRouteData();
            var id = Convert.ToInt32(routeData.Values["id"]);

            var vm = request.GetComposedResponseModel();
            vm.AValue = id;

            return Task.CompletedTask;
        }
    }
}