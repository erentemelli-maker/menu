using DijitalMenu.Application;
using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record SalesReportViewModel(SalesReportDto Report);

public sealed record OrderHistoryViewModel(
    IReadOnlyList<Order> Orders,
    DateTime? From,
    DateTime? To,
    int? TableNumber,
    OrderStatus? Status);
