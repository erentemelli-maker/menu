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

    public IActionResult Index(int branchId = 1, int tableNumber = 5)
    {
        if (menuService.GetTable(tableNumber) is not { IsActive: true })
        {
            return NotFound("Masa bulunamadi.");
        }

        return View(BuildViewModel(branchId, tableNumber));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int branchId, int tableNumber, int productId)
    {
        var cart = GetCart(branchId, tableNumber);
        cart.RemoveAll(item => item.ProductId == productId);
        HttpContext.Session.SetJson(GetCartKey(branchId, tableNumber), cart);
        return RedirectToAction(nameof(Index), new { branchId, tableNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(
        int branchId,
        int tableNumber,
        int productId,
        int quantity,
        string? note,
        IReadOnlyList<int>? extraIds)
    {
        var cart = GetCart(branchId, tableNumber);
        var index = cart.FindIndex(item => item.ProductId == productId);
        if (index >= 0)
        {
            cart[index] = cart[index] with
            {
                Quantity = Math.Clamp(quantity, 1, 20),
                Note = note?.Trim() ?? string.Empty,
                ExtraIds = extraIds?.Distinct().ToList() ?? []
            };
            HttpContext.Session.SetJson(GetCartKey(branchId, tableNumber), cart);
        }

        return RedirectToAction(nameof(Index), new { branchId, tableNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int branchId, int tableNumber)
    {
        try
        {
            var order = orderService.Create(
                tableNumber,
                GetCart(branchId, tableNumber).Select(item =>
                    new OrderLineRequest(item.ProductId, item.Quantity, item.Note, item.ExtraIds)));

            HttpContext.Session.Remove(GetCartKey(branchId, tableNumber));
            await hubContext.Clients.All.SendAsync("OrderCreated", new
            {
                order.Id,
                order.TableNumber,
                order.CreatedAt,
                order.Total
            });
            TempData["Message"] = $"Siparisiniz alindi. Siparis no: #{order.Id}";
            return RedirectToAction("Index", "Menu", new { branchId, tableNumber });
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index), new { branchId, tableNumber });
        }
    }

    private List<CartItemSession> GetCart(int branchId, int tableNumber) =>
        HttpContext.Session.GetJson<List<CartItemSession>>(GetCartKey(branchId, tableNumber)) ?? [];

    private CartViewModel BuildViewModel(int branchId, int tableNumber)
    {
        var lines = GetCart(branchId, tableNumber)
            .Select(item => new { Item = item, Product = menuService.GetProduct(item.ProductId) })
            .Where(item => item.Product is not null)
            .Select(item => new CartLineViewModel(
                item.Item.ProductId,
                item.Product!.Name,
                item.Product.Price,
                item.Item.Quantity,
                item.Item.Note,
                item.Product.Extras
                    .Select(extra => new CartExtraViewModel(
                        extra.Id,
                        extra.Name,
                        extra.Price,
                        item.Item.ExtraIds?.Contains(extra.Id) == true))
                    .ToList()))
            .ToList();

        return new CartViewModel(branchId, tableNumber, lines);
    }

    private static string GetCartKey(int branchId, int tableNumber) => $"{CartKey}:{branchId}:{tableNumber}";
}
