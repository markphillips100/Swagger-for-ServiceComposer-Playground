﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;
using ServiceComposer.OpenApi.Attributes;

namespace Swagger_for_ServiceComposer.Handlers.ServiceB
{
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        [ProducesCompositionResponseType("AnotherValue", typeof(string))]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ApiParameterDescription(Name = "id", IsRequired = true, Type = typeof(int), Source = "Path")]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();
            vm.AnotherValue = "Hi, there.";

            return Task.CompletedTask;
        }
    }
}