using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminTablesController(IAdminService adminService) : Controller
{
    public IActionResult Index(int? editId = null)
    {
        var table = editId.HasValue ? adminService.GetTable(editId.Value) : null;

        return View(new AdminTablesViewModel(
            adminService.GetTables(),
            new TableInput
            {
                Id = table?.Id,
                Number = table?.Number ?? 0,
                IsActive = table?.IsActive ?? true
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Table")] TableInput input)
    {
        try
        {
            adminService.SaveTable(input.Id, input.Number, input.IsActive);
            TempData["Message"] = "Masa kaydedildi.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        TempData[adminService.DeleteTable(id) ? "Message" : "Error"] = "Masa silindi.";
        return RedirectToAction(nameof(Index));
    }
}
