using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;
using ServiceComposer.AspNetCore.TypedViewModel;
using Swagger_for_ServiceComposer.ApiDescription;
using Swagger_for_ServiceComposer.Models.Response;

namespace Swagger_for_ServiceComposer.Handlers.ServiceB
{
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        [TypedViewModel(typeof(IAnotherValue))]
        [ApiParameterDescription(Name = "id", IsRequired = true, Source = "Path", Type = typeof(int))]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel<IAnotherValue>();
            vm.AnotherValue = "Hi, there.";

            return Task.CompletedTask;
        }
    }
}