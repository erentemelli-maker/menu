using System.Security.Claims;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

public sealed class AccountController : Controller
{
    private static readonly IReadOnlyDictionary<string, DemoUser> Users =
        new Dictionary<string, DemoUser>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = new("admin123", "Admin", "İşletme Yöneticisi"),
            ["garson"] = new("garson123", "Garson", "Servis Ekibi"),
            ["mutfak"] = new("mutfak123", "Mutfak", "Mutfak Ekibi")
        };

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "AdminOrders");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid ||
            !Users.TryGetValue(model.Username.Trim(), out var user) ||
            user.Password != model.Password)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, model.Username.Trim()),
            new(ClaimTypes.Role, user.Role),
            new("display_name", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToAction("Index", "AdminOrders");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private sealed record DemoUser(string Password, string Role, string DisplayName);
}
