using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantia.Data;
using Quantia.Models;
using System.Security.Claims;

namespace Quantia.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return RedirectToAction("Login", "Account");

        var user = await _context.Users.FindAsync(int.Parse(userId));
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UserModel updated)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return RedirectToAction("Login", "Account");

        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user is null) return RedirectToAction("Login", "Account");

        user.LastName = updated.LastName;
        user.Email = updated.Email;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Changes saved.";
        return RedirectToAction("Index");
    }
}
