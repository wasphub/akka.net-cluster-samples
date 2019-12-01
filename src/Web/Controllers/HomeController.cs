using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Web.Services;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly References _references;

        public HomeController(ILogger<HomeController> logger, References references)
        {
            _logger = logger;
            _references = references;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [HttpPost("/job")]
        public async Task Job()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var input = await reader.ReadToEndAsync();

                _references.Worker.Tell(new Common.Commands.Command(input, _references.Watcher));
            }
        }
    }
}
