using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;
using Swagger_for_ServiceComposer.Models.Response;

namespace Swagger_for_ServiceComposer.Models
{
    public class ViewModelFactory : IViewModelFactory
    {
        public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
        {
            return new ViewModel();
        }
    }
}