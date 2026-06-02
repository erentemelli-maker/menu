using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DijitalMenu.WebUI.Controllers;

public sealed class TableServiceController(
    IOperationService operationService,
    IHubContext<OrderHub> hubContext) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int branchId, int tableNumber, ServiceRequestType type)
    {
        try
        {
            operationService.CreateServiceRequest(tableNumber, type);
            await hubContext.Clients.All.SendAsync("TableServiceRequested", new { tableNumber, type = type.ToString() });
            TempData["Message"] = type == ServiceRequestType.RequestBill
                ? "Hesap isteğiniz servis ekibine iletildi."
                : "Garson çağrınız servis ekibine iletildi.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction("Index", "Menu", new { branchId, tableNumber });
    }
}
