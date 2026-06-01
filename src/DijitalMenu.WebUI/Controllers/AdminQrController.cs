using System.Text;
using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminQrController(IAdminService adminService) : Controller
{
    public IActionResult Index()
    {
        var tables = adminService.GetTables()
            .Where(table => table.IsActive)
            .Select(table =>
            {
                var menuUrl = Url.Action("Index", "Menu", new { tableNumber = table.Number }, Request.Scheme)
                    ?? string.Empty;

                using var qrData = QRCodeGenerator.GenerateQrCode(menuUrl, QRCodeGenerator.ECCLevel.Q);
                var svg = new SvgQRCode(qrData).GetGraphic(4);
                return new TableQrViewModel(table.Number, menuUrl, svg);
            })
            .ToList();

        return View(tables);
    }

    public IActionResult Download(int tableNumber)
    {
        var table = adminService.GetTables().SingleOrDefault(item => item.Number == tableNumber);
        if (table is not { IsActive: true })
        {
            return NotFound();
        }

        var menuUrl = Url.Action("Index", "Menu", new { tableNumber }, Request.Scheme) ?? string.Empty;
        using var qrData = QRCodeGenerator.GenerateQrCode(menuUrl, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(qrData).GetGraphic(8);
        return File(Encoding.UTF8.GetBytes(svg), "image/svg+xml", $"masa-{tableNumber}-qr.svg");
    }
}
