using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminStaffController(IStaffService staffService) : Controller
{
    public IActionResult Index(int? editId = null)
    {
        var user = editId.HasValue ? staffService.GetUser(editId.Value) : null;
        return View(new AdminStaffViewModel(
            staffService.GetUsers(),
            new StaffInput
            {
                Id = user?.Id,
                Username = user?.Username ?? string.Empty,
                DisplayName = user?.DisplayName ?? string.Empty,
                Role = user?.Role ?? StaffRole.Waiter,
                IsActive = user?.IsActive ?? true
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Staff")] StaffInput input)
    {
        try
        {
            staffService.Save(
                input.Id,
                input.Username,
                input.DisplayName,
                input.Role,
                input.IsActive,
                input.Password);
            TempData["Message"] = "Personel kaydedildi.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { editId = input.Id });
    }
}
