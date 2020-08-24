using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace Swagger_for_ServiceComposer.Handlers.ServiceA
{
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
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