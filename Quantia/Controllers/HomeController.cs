using Microsoft.AspNetCore.Mvc;

namespace Quantia.Controllers;

public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["RequestId"] = HttpContext.TraceIdentifier;
        return View("~/Views/Shared/Error.cshtml");
    }
}
