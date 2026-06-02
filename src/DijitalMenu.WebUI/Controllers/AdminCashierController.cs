using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Hubs;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminCashierController(
    IOperationService operationService,
    IHubContext<OrderHub> hubContext) : Controller
{
    public IActionResult Index() =>
        View(new CashierViewModel(
            operationService.GetOpenTableAccounts(),
            operationService.GetTables()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectPayment(
        int tableNumber,
        PaymentMethod method,
        string? amount,
        string? remainingTotal,
        int[]? productId,
        string[]? unitPrice,
        int[]? quantity)
    {
        var selectedLines = new List<PaymentLineRequest>();
        for (var index = 0; index < (productId?.Length ?? 0); index++)
        {
            if (unitPrice is null || quantity is null ||
                index >= unitPrice.Length || index >= quantity.Length ||
                quantity[index] <= 0 ||
                !decimal.TryParse(
                    unitPrice[index],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedPrice))
            {
                continue;
            }

            selectedLines.Add(new PaymentLineRequest(productId![index], parsedPrice, quantity[index]));
        }
        var productTotal = selectedLines.Sum(line => line.UnitPrice * line.Quantity);
        var parsedRemainingTotal = ParseAmount(remainingTotal);
        var paymentAmount = productTotal > 0
            ? productTotal
            : ParseAmount(amount) ?? parsedRemainingTotal ?? 0;

        if (!operationService.CollectTablePayment(tableNumber, paymentAmount, method, selectedLines))
        {
            TempData["Error"] = "Ödeme alınamadı. Tutarı ve kalan bakiyeyi kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        await hubContext.Clients.All.SendAsync("TablePaymentCollected", new { tableNumber });
        TempData["Message"] = paymentAmount == parsedRemainingTotal
            ? $"Masa {tableNumber} hesabı kapatıldı."
            : $"Masa {tableNumber} için {paymentAmount:N2} ₺ tahsil edildi.";
        return RedirectToAction(nameof(Index));
    }

    private static decimal? ParseAmount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
}
