using DijitalMenu.Application;
using DijitalMenu.WebUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DijitalMenu.WebUI.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminReportsController(IReportService reportService) : Controller
{
    public IActionResult Index(DateTime? from, DateTime? to)
    {
        var effectiveTo = (to ?? DateTime.Today).Date;
        var effectiveFrom = (from ?? effectiveTo.AddDays(-6)).Date;

        return View(new SalesReportViewModel(reportService.GetSalesReport(effectiveFrom, effectiveTo)));
    }
}
