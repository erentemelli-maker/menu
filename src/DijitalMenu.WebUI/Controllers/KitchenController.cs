using DijitalMenu.Application;
using DijitalMenu.WebUI.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin,Mutfak")]
public sealed class KitchenController(IOperationService operationService, IHubContext<OrderHub> hubContext) : Controller
{
    public IActionResult Index() => View(operationService.GetKitchenOrders());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Advance(int orderId)
    {
        if (!operationService.AdvanceKitchenOrder(orderId))
        {
            return NotFound();
        }

        await hubContext.Clients.All.SendAsync("OrderStatusChanged", new { orderId });
        return RedirectToAction(nameof(Index));
    }
}
