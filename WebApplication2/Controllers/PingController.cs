using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            // Devuelve un simple mensaje JSON
            return Ok(new { message = "pong", timestamp = DateTime.UtcNow });
        }
    }
}
