using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Quantia.Controllers;

[Authorize]
public class StrategyController : Controller
{
    public IActionResult CreateStrategy() => View();
}
