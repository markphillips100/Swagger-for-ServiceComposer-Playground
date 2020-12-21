using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;
using Swagger_for_ServiceComposer.ApiDescription;
using Swagger_for_ServiceComposer.Models.Response;

namespace Swagger_for_ServiceComposer.Handlers.Documentation
{
    public class DocumentationHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        [ProducesDefaultResponseType(typeof(void))]
        [ProducesResponseType(typeof(ViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ApiParameterDescription(Name = "id", IsRequired = true, Type = typeof(int), Source = "Path")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
}