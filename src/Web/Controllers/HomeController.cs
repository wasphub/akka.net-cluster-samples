using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    }
}
