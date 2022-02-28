using System.Text;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace ServiceComposerSourceGenerators.UnitTests;

[UsesVerify]
public class SwaggerGeneratorTests
{
    [Fact]
    public Task CanGenerateNamespacedClassNames()
    {
        var inputBuilder = new StringBuilder();
        inputBuilder.Append(CompositionHandlerInterface);
        inputBuilder.AppendLine();
        inputBuilder.Append(ServiceASampleHandlerClass);
        inputBuilder.AppendLine();
        inputBuilder.Append(ServiceBSampleHandlerClass);
        var input = inputBuilder.ToString();

        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<ServiceComposerSwaggerSourceGenerator>(input);

        Assert.Empty(diagnostics);
        return Verifier.Verify(output).UseDirectory("Snapshots");
    }

    private static readonly string CompositionHandlerInterface = $@"
namespace Swagger_for_ServiceComposer
{{
    public interface ICompositionRequestsHandler
    {{
        Task Handle(HttpRequest request);
    }}
}}";

    private static readonly string ServiceASampleHandlerClass = $@"
namespace Swagger_for_ServiceComposer.Handlers.ServiceA
{{
    public class SampleHandler : ICompositionRequestsHandler
    {{
        [HttpGet(""/sample/{{id}}"")]
        [ProducesCompositionResponseType(""AValue"", typeof(int))]
        public Task Handle(HttpRequest request)
        {{
            var routeData = request.HttpContext.GetRouteData();
            var id = Convert.ToInt32(routeData.Values[""id""]);

            var vm = request.GetComposedResponseModel();
            vm.AValue = id;

            return Task.CompletedTask;
        }}
    }}
}}";

    private static readonly string ServiceBSampleHandlerClass = $@"
namespace Swagger_for_ServiceComposer.Handlers.ServiceB
{{
    public class MyModel
    {{
        public int MyProp {{ get; set; }}
    }}

    public class SampleHandler : ICompositionRequestsHandler
    {{
        [HttpGet(""/sample/{{id}}"")]
        [ProducesCompositionResponseType(""AnotherValue"", typeof(MyModel))]
        public Task Handle(HttpRequest request)
        {{
            var vm = request.GetComposedResponseModel();
            // vm.AnotherValue = ""Hi, there."";
            vm.AnotherValue = new MyModel {{ MyProp = 1 }}

            return Task.CompletedTask;
        }}
    }}
}}";

    
}
