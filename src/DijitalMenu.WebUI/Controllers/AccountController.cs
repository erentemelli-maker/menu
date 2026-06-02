using System.Security.Claims;
using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using StaffAuthenticationService = DijitalMenu.Application.IAuthenticationService;
using DijitalMenu.WebUI.Presentation;
using DijitalMenu.WebUI.Services;

namespace DijitalMenu.WebUI.Controllers;

public sealed class AccountController(
    StaffAuthenticationService authenticationService,
    IStaffService staffService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleHome();
        }

        return View(BuildLoginViewModel(returnUrl: returnUrl));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid ||
            authenticationService.Authenticate(model.Username, model.Password) is not { } user)
        {
            ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
            return View(BuildLoginViewModel(model));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, GetClaimRole(user.Role)),
            new("display_name", user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        HttpContext.Session.SetInt32(SessionBranchContext.SessionKey, user.BranchId);

        return Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToRoleHome(user.Role);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToRoleHome() =>
        User.IsInRole("Admin") ? RedirectToAction("Index", "AdminDashboard") :
        User.IsInRole("Garson") ? RedirectToAction("Index", "Waiter") :
        RedirectToAction("Index", "Kitchen");

    private IActionResult RedirectToRoleHome(StaffRole role) => role switch
    {
        StaffRole.Admin => RedirectToAction("Index", "AdminDashboard"),
        StaffRole.Waiter => RedirectToAction("Index", "Waiter"),
        StaffRole.Kitchen => RedirectToAction("Index", "Kitchen"),
        _ => throw new InvalidOperationException("Tanımsız personel rolü.")
    };

    private static string GetClaimRole(StaffRole role) => role switch
    {
        StaffRole.Admin => "Admin",
        StaffRole.Waiter => "Garson",
        StaffRole.Kitchen => "Mutfak",
        _ => throw new InvalidOperationException("Tanımsız personel rolü.")
    };

    private LoginViewModel BuildLoginViewModel(LoginViewModel? model = null, string? returnUrl = null)
    {
        model ??= new LoginViewModel { ReturnUrl = returnUrl };
        model.StaffOptions = staffService.GetUsers()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .Select(user => new LoginStaffOptionViewModel(
                user.Username,
                user.DisplayName,
                user.Role.ToTurkish()))
            .ToList();
        return model;
    }
}
