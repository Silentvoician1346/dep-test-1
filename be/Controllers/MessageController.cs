using Microsoft.AspNetCore.Mvc;

namespace be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    [HttpGet]
    [Produces("text/plain")]
    public ContentResult Get()
    {
        return Content("Hello from ASP.NET Core", "text/plain");
    }
}
