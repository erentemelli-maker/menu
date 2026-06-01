using DijitalMenu.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController(IReportService reportService) : Controller
{
    public IActionResult Index() => View(reportService.GetDashboard(DateTime.Today));
}
