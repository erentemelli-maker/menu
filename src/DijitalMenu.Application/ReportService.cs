using DijitalMenu.Domain;

namespace DijitalMenu.Application;

public interface IReportService
{
    DashboardDto GetDashboard(DateTime today);
    SalesReportDto GetSalesReport(DateTime from, DateTime to);
    IReadOnlyList<Order> GetOrderHistory(DateTime? from, DateTime? to, int? tableNumber, OrderStatus? status);
}

public sealed record DashboardDto(
    decimal DailyRevenue,
    int DailyOrderCount,
    int ActiveOrderCount,
    int ServiceWaitingTableCount,
    decimal AverageOrderValue,
    IReadOnlyList<ProductSalesDto> TopProducts,
    IReadOnlyList<DailySalesDto> LastSevenDays);

public sealed record SalesReportDto(
    DateTime From,
    DateTime To,
    decimal TotalRevenue,
    int OrderCount,
    decimal AverageOrderValue,
    IReadOnlyList<DailySalesDto> DailySales,
    IReadOnlyList<ProductSalesDto> ProductSales);

public sealed record ProductSalesDto(int ProductId, string ProductName, int Quantity, decimal Revenue);
public sealed record DailySalesDto(DateTime Date, int OrderCount, decimal Revenue);

public sealed class ReportService(IRestaurantRepository repository) : IReportService
{
    public DashboardDto GetDashboard(DateTime today)
    {
        var dayStart = today.Date;
        var dayEnd = dayStart.AddDays(1);
        var orders = repository.GetOrders();
        var dailyOrders = orders
            .Where(order => order.CreatedAt >= dayStart && order.CreatedAt < dayEnd)
            .ToList();
        var completedDailyOrders = dailyOrders
            .Where(order => order.Status == OrderStatus.Delivered)
            .ToList();

        return new DashboardDto(
            completedDailyOrders.Sum(order => order.Total),
            dailyOrders.Count,
            orders.Count(order => order.Status != OrderStatus.Delivered),
            repository.GetTables().Count(table => table.Status == TableStatus.ServiceWaiting),
            AverageTotal(completedDailyOrders),
            GetProductSales(completedDailyOrders).Take(5).ToList(),
            GetDailySales(orders, dayStart.AddDays(-6), dayStart));
    }

    public SalesReportDto GetSalesReport(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;
        if (start > end)
        {
            (start, end) = (end, start);
        }

        var completedOrders = repository.GetOrders()
            .Where(order => order.Status == OrderStatus.Delivered &&
                            order.CreatedAt >= start &&
                            order.CreatedAt < end.AddDays(1))
            .ToList();

        return new SalesReportDto(
            start,
            end,
            completedOrders.Sum(order => order.Total),
            completedOrders.Count,
            AverageTotal(completedOrders),
            GetDailySales(completedOrders, start, end),
            GetProductSales(completedOrders));
    }

    public IReadOnlyList<Order> GetOrderHistory(
        DateTime? from,
        DateTime? to,
        int? tableNumber,
        OrderStatus? status)
    {
        var orders = repository.GetOrders().AsEnumerable();

        if (from.HasValue)
        {
            orders = orders.Where(order => order.CreatedAt >= from.Value.Date);
        }

        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            orders = orders.Where(order => order.CreatedAt < end);
        }

        if (tableNumber.HasValue)
        {
            orders = orders.Where(order => order.TableNumber == tableNumber.Value);
        }

        if (status.HasValue)
        {
            orders = orders.Where(order => order.Status == status.Value);
        }

        return orders.OrderByDescending(order => order.CreatedAt).ToList();
    }

    private static decimal AverageTotal(IReadOnlyCollection<Order> orders) =>
        orders.Count == 0 ? 0 : orders.Average(order => order.Total);

    private static IReadOnlyList<ProductSalesDto> GetProductSales(IEnumerable<Order> orders) =>
        orders
            .SelectMany(order => order.Items)
            .GroupBy(item => new { item.ProductId, item.ProductName })
            .Select(group => new ProductSalesDto(
                group.Key.ProductId,
                group.Key.ProductName,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.LineTotal)))
            .OrderByDescending(product => product.Revenue)
            .ThenBy(product => product.ProductName)
            .ToList();

    private static IReadOnlyList<DailySalesDto> GetDailySales(
        IEnumerable<Order> orders,
        DateTime from,
        DateTime to)
    {
        var completedByDay = orders
            .Where(order => order.Status == OrderStatus.Delivered)
            .GroupBy(order => order.CreatedAt.Date)
            .ToDictionary(
                group => group.Key,
                group => new DailySalesDto(group.Key, group.Count(), group.Sum(order => order.Total)));

        return Enumerable.Range(0, (to.Date - from.Date).Days + 1)
            .Select(offset => from.Date.AddDays(offset))
            .Select(date => completedByDay.GetValueOrDefault(date) ?? new DailySalesDto(date, 0, 0))
            .ToList();
    }
}
