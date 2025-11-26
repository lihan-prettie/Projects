using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Scheduling.Helpers;
using System.Threading.Tasks;

namespace Scheduling.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HolidayController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public HolidayController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("{year}")]
        public async Task<IActionResult> Get(int year) {
            var json = await HolidayHelper.LoadHolidaysAsync(year,_env);
            return Content(json,"application/json");
        }
    }
}
