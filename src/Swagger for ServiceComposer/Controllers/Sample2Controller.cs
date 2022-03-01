using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Swagger_for_ServiceComposer.Controllers;

public class Sample2ControllerViewModel
{
    public string AString { get; set; }
}

public class Sample2Controller : Controller
{
        
    [HttpGet("/sample2/{id}")]
    [ProducesResponseType(typeof(Sample2ControllerViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Sample2ControllerViewModel> Get(string id)
    {
        var model = new Sample2ControllerViewModel() {AString = "Some string"};

        return Ok(model);
    }
}