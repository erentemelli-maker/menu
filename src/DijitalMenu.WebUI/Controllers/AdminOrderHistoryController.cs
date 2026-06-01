using DijitalMenu.Application;
using DijitalMenu.Domain;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminOrderHistoryController(IReportService reportService) : Controller
{
    public IActionResult Index(DateTime? from, DateTime? to, int? tableNumber, OrderStatus? status) =>
        View(new OrderHistoryViewModel(
            reportService.GetOrderHistory(from, to, tableNumber, status),
            from,
            to,
            tableNumber,
            status));
}
