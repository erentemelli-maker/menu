using DijitalMenu.Application;
using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record WaiterViewModel(
    IReadOnlyList<Order> ReadyOrders,
    IReadOnlyList<RestaurantTable> Tables,
    IReadOnlyList<TableServiceRequest> ServiceRequests);

public sealed record CashierViewModel(
    IReadOnlyList<TableAccountDto> OpenAccounts,
    IReadOnlyList<RestaurantTable> Tables);
