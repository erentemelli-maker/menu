using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record WaiterViewModel(
    IReadOnlyList<Order> ReadyOrders,
    IReadOnlyList<RestaurantTable> Tables);
