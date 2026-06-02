using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using DijitalMenu.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminBranchesController(IBranchService branchService) : Controller
{
    public IActionResult Index(int? editId = null)
    {
        var branch = editId.HasValue ? branchService.GetBranch(editId.Value) : null;
        return View(new AdminBranchesViewModel(
            branchService.GetBranches(),
            new BranchInput
            {
                Id = branch?.Id,
                Name = branch?.Name ?? string.Empty,
                IsActive = branch?.IsActive ?? true
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Branch")] BranchInput input)
    {
        try
        {
            branchService.Save(input.Id, input.Name, input.IsActive);
            TempData["Message"] = "Şube kaydedildi.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { editId = input.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Select(int branchId, string? returnUrl = null)
    {
        if (branchService.GetBranch(branchId) is not { IsActive: true })
        {
            return NotFound();
        }

        HttpContext.Session.SetInt32(SessionBranchContext.SessionKey, branchId);
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
