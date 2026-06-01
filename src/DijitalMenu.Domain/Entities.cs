namespace DijitalMenu.Domain;

public sealed class RestaurantTable
{
    public int Id { get; set; }
    public int Number { get; set; }
    public bool IsActive { get; set; } = true;
    public TableStatus Status { get; set; } = TableStatus.Available;
}

public enum TableStatus
{
    Available,
    Occupied,
    ServiceWaiting
}

public sealed class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public enum OrderStatus
{
    New,
    Preparing,
    Ready,
    Delivered
}

public sealed class Order
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total => Items.Sum(item => item.LineTotal);
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
