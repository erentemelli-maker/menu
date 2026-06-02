namespace DijitalMenu.Domain;

public sealed class Branch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RestaurantTable
{
    public int Id { get; set; }
    public int BranchId { get; set; }
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
    public int BranchId { get; set; }
    public int CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public sealed class ProductExtra
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
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
    public int BranchId { get; set; }
    public int TableNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public bool IsPaid { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public decimal Total => Items.Sum(item => item.LineTotal);
}

public enum PaymentMethod
{
    Cash,
    Card
}

public sealed class Payment
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int TableNumber { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaidAt { get; set; }
    public List<PaymentItem> Items { get; set; } = [];
}

public sealed class PaymentItem
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public sealed class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Note { get; set; } = string.Empty;
    public string ExtrasSummary { get; set; } = string.Empty;
    public decimal LineTotal => UnitPrice * Quantity;
}

public enum ServiceRequestType
{
    CallWaiter,
    RequestBill
}

public sealed class TableServiceRequest
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int TableNumber { get; set; }
    public ServiceRequestType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
}

public enum StaffRole
{
    Admin,
    Waiter,
    Kitchen
}

public sealed class StaffUser
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public StaffRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
