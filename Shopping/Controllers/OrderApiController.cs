using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    [Authorize]
    [Route("api/orders")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {

    }
}
