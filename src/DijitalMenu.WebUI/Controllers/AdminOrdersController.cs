using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminOrdersController(IOrderService orderService, IHubContext<OrderHub> hubContext) : Controller
{
    public IActionResult Index() => View(orderService.GetOrders());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus status)
    {
        if (!orderService.UpdateStatus(orderId, status))
        {
            return NotFound();
        }

        await hubContext.Clients.All.SendAsync("OrderStatusChanged", new { orderId, status = status.ToString() });
        return RedirectToAction(nameof(Index));
    }
}
