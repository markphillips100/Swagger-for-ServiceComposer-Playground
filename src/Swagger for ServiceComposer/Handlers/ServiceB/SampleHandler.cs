using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Swagger_for_ServiceComposer.Handlers.ServiceB
{
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();
            vm.AnotherValue = "Hi, there.";

            return Task.CompletedTask;
        }
    }
}