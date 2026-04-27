using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using Quantia.Models;
using System.Security.Claims;

namespace Quantia.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(UserModel user)
    {
        if (!ModelState.IsValid)
            return View(user);

        var emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "This email is already in use.");
            return View(user);
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            await HttpContext.SignOutAsync("QuantiaAuth");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel login)
    {
        if (!ModelState.IsValid)
            return View(login);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
        if (userInDb is null || !BCrypt.Net.BCrypt.Verify(login.Password, userInDb.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(login);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInDb.Id.ToString()),
            new(ClaimTypes.Name, userInDb.LastName),
            new(ClaimTypes.Email, userInDb.Email)
        };

        var identity = new ClaimsIdentity(claims, "QuantiaAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("QuantiaAuth", principal);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("QuantiaAuth");
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    public IActionResult Index() => View();
}
