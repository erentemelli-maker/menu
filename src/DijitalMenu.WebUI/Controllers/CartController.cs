using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using DijitalMenu.WebUI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

public sealed class CartController(
    IMenuService menuService,
    IOrderService orderService,
    IHubContext<OrderHub> hubContext) : Controller
{
    private const string CartKey = "cart";

    public IActionResult Index(int tableNumber = 5)
    {
        if (menuService.GetTable(tableNumber) is not { IsActive: true })
        {
            return NotFound("Masa bulunamadi.");
        }

        return View(BuildViewModel(tableNumber));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int tableNumber, int productId)
    {
        var cart = GetCart();
        cart.RemoveAll(item => item.ProductId == productId);
        HttpContext.Session.SetJson(CartKey, cart);
        return RedirectToAction(nameof(Index), new { tableNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int tableNumber)
    {
        try
        {
            var order = orderService.Create(
                tableNumber,
                GetCart().Select(item => new OrderLineRequest(item.ProductId, item.Quantity)));

            HttpContext.Session.Remove(CartKey);
            await hubContext.Clients.All.SendAsync("OrderCreated", new
            {
                order.Id,
                order.TableNumber,
                order.CreatedAt,
                order.Total
            });
            TempData["Message"] = $"Siparisiniz alindi. Siparis no: #{order.Id}";
            return RedirectToAction("Index", "Menu", new { tableNumber });
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index), new { tableNumber });
        }
    }

    private List<CartItemSession> GetCart() =>
        HttpContext.Session.GetJson<List<CartItemSession>>(CartKey) ?? [];

    private CartViewModel BuildViewModel(int tableNumber)
    {
        var lines = GetCart()
            .Select(item => new { Item = item, Product = menuService.GetProduct(item.ProductId) })
            .Where(item => item.Product is not null)
            .Select(item => new CartLineViewModel(
                item.Item.ProductId,
                item.Product!.Name,
                item.Product.Price,
                item.Item.Quantity))
            .ToList();

        return new CartViewModel(tableNumber, lines);
    }
}
