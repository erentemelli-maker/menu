using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminCatalogController(IAdminService adminService) : Controller
{
    public IActionResult Index(int? editCategoryId = null)
    {
        var category = editCategoryId.HasValue
            ? adminService.GetCategory(editCategoryId.Value)
            : null;

        return View(new AdminCatalogViewModel(
            adminService.GetCategories(),
            adminService.GetProducts(),
            new CategoryInput
            {
                Id = category?.Id,
                Name = category?.Name ?? string.Empty,
                DisplayOrder = category?.DisplayOrder ?? 0
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveCategory([Bind(Prefix = "Category")] CategoryInput input)
    {
        try
        {
            adminService.SaveCategory(input.Id, input.Name, input.DisplayOrder);
            TempData["Message"] = "Kategori kaydedildi.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteCategory(int id)
    {
        TempData[adminService.DeleteCategory(id) ? "Message" : "Error"] =
            adminService.GetCategory(id) is null
                ? "Kategori silindi."
                : "Bu kategoride urun oldugu icin kategori silinemedi.";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult EditProduct(int? id = null)
    {
        var product = id.HasValue ? adminService.GetProduct(id.Value) : null;
        if (id.HasValue && product is null)
        {
            return NotFound();
        }

        return View(new ProductFormViewModel(
            adminService.GetCategories(),
            new ProductInput
            {
                Id = product?.Id,
                CategoryId = product?.CategoryId ?? adminService.GetCategories().FirstOrDefault()?.Id ?? 0,
                Name = product?.Name ?? string.Empty,
                Description = product?.Description ?? string.Empty,
                ImageUrl = product?.ImageUrl ?? string.Empty,
                Price = product?.Price ?? 0,
                IsAvailable = product?.IsAvailable ?? true
            }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveProduct([Bind(Prefix = "Product")] ProductInput input)
    {
        try
        {
            adminService.SaveProduct(
                input.Id,
                input.CategoryId,
                input.Name,
                input.Description,
                input.ImageUrl,
                input.Price,
                input.IsAvailable);

            TempData["Message"] = "Urun kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(EditProduct), new { id = input.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProduct(int id)
    {
        TempData[adminService.DeleteProduct(id) ? "Message" : "Error"] =
            "Urun silindi.";

        return RedirectToAction(nameof(Index));
    }
}
