using DijitalMenu.Application;
using DijitalMenu.WebUI.Hubs;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Garson")]
public sealed class WaiterController(IOperationService operationService, IHubContext<OrderHub> hubContext) : Controller
{
    public IActionResult Index() =>
        View(new WaiterViewModel(
            operationService.GetReadyOrders(),
            operationService.GetTables(),
            operationService.GetActiveServiceRequests()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliver(int orderId)
    {
        if (!operationService.DeliverOrder(orderId))
        {
            return NotFound();
        }

        await hubContext.Clients.All.SendAsync("OrderStatusChanged", new { orderId });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveRequest(int id)
    {
        if (!operationService.ResolveServiceRequest(id))
        {
            return NotFound();
        }

        await hubContext.Clients.All.SendAsync("TableServiceResolved", new { id });
        return RedirectToAction(nameof(Index));
    }

}
