using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Quantia.Controllers;

[Authorize]
public class FeedbackController : Controller
{
    public IActionResult Index() => View();
}
