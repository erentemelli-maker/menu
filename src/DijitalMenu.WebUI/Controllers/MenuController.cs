using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

public sealed class MenuController(IMenuService menuService) : Controller
{
    private const string CartKey = "cart";

    public IActionResult Index(int tableNumber = 5)
    {
        if (menuService.GetTable(tableNumber) is not { IsActive: true })
        {
            return NotFound("Masa bulunamadi.");
        }

        return View(new MenuViewModel(tableNumber, menuService.GetMenu()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int tableNumber, int productId)
    {
        if (menuService.GetTable(tableNumber) is not { IsActive: true } ||
            menuService.GetProduct(productId) is null)
        {
            return NotFound();
        }

        var cart = HttpContext.Session.GetJson<List<CartItemSession>>(CartKey) ?? [];
        var existing = cart.FindIndex(item => item.ProductId == productId);

        if (existing >= 0)
        {
            cart[existing] = cart[existing] with { Quantity = cart[existing].Quantity + 1 };
        }
        else
        {
            cart.Add(new CartItemSession(productId, 1));
        }

        HttpContext.Session.SetJson(CartKey, cart);
        TempData["Message"] = "Urun sepete eklendi.";
        return RedirectToAction(nameof(Index), new { tableNumber });
    }
}
