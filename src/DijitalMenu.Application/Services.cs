using DijitalMenu.Domain;

namespace DijitalMenu.Application;

public interface IBranchContext
{
    int BranchId { get; }
}

public interface IRestaurantRepository
{
    IReadOnlyList<Branch> GetBranches();
    Branch? GetBranch(int id);
    Branch AddBranch(Branch branch);
    bool UpdateBranch(Branch branch);
    RestaurantTable? GetTable(int tableNumber);
    IReadOnlyList<RestaurantTable> GetTables();
    RestaurantTable? GetTableById(int id);
    RestaurantTable AddTable(RestaurantTable table);
    bool UpdateTable(RestaurantTable table);
    bool DeleteTable(int id);
    IReadOnlyList<Category> GetCategories();
    Category? GetCategory(int id);
    Category AddCategory(Category category);
    bool UpdateCategory(Category category);
    bool DeleteCategory(int id);
    IReadOnlyList<Product> GetProducts();
    Product? GetProduct(int productId);
    Product AddProduct(Product product);
    bool UpdateProduct(Product product);
    bool DeleteProduct(int id);
    IReadOnlyList<ProductExtra> GetProductExtras(int productId);
    IReadOnlyList<Order> GetOrders();
    Order AddOrder(Order order);
    bool UpdateOrderStatus(int orderId, OrderStatus status);
    IReadOnlyList<Payment> GetPayments();
    bool CollectTablePayment(int tableNumber, decimal amount, PaymentMethod method, IReadOnlyList<PaymentLineRequest>? lines = null);
    IReadOnlyList<StaffUser> GetStaffUsers();
    StaffUser? GetStaffUser(int id);
    StaffUser? GetStaffUser(string username);
    StaffUser AddStaffUser(StaffUser user);
    bool UpdateStaffUser(StaffUser user);
    IReadOnlyList<TableServiceRequest> GetActiveServiceRequests();
    TableServiceRequest AddServiceRequest(TableServiceRequest request);
    bool ResolveServiceRequest(int id);
}

public interface IBranchService
{
    IReadOnlyList<Branch> GetBranches();
    Branch? GetBranch(int id);
    Branch Save(int? id, string name, bool isActive);
}

public interface IMenuService
{
    RestaurantTable? GetTable(int tableNumber);
    IReadOnlyList<MenuCategoryDto> GetMenu();
    ProductDto? GetProduct(int productId);
}

public interface IOrderService
{
    Order Create(int tableNumber, IEnumerable<OrderLineRequest> lines);
    IReadOnlyList<Order> GetOrders();
    bool UpdateStatus(int orderId, OrderStatus status);
}

public interface IOperationService
{
    IReadOnlyList<Order> GetKitchenOrders();
    IReadOnlyList<Order> GetReadyOrders();
    IReadOnlyList<RestaurantTable> GetTables();
    bool AdvanceKitchenOrder(int orderId);
    bool DeliverOrder(int orderId);
    IReadOnlyList<TableServiceRequest> GetActiveServiceRequests();
    TableServiceRequest CreateServiceRequest(int tableNumber, ServiceRequestType type);
    bool ResolveServiceRequest(int id);
    IReadOnlyList<TableAccountDto> GetOpenTableAccounts();
    bool CollectTablePayment(int tableNumber, decimal amount, PaymentMethod method, IReadOnlyList<PaymentLineRequest>? lines = null);
}

public interface IAdminService
{
    IReadOnlyList<Category> GetCategories();
    Category? GetCategory(int id);
    Category SaveCategory(int? id, string name, int displayOrder);
    bool DeleteCategory(int id);
    IReadOnlyList<Product> GetProducts();
    Product? GetProduct(int id);
    Product SaveProduct(int? id, int categoryId, string name, string description, string imageUrl, decimal price, bool isAvailable);
    bool DeleteProduct(int id);
    IReadOnlyList<RestaurantTable> GetTables();
    RestaurantTable? GetTable(int id);
    RestaurantTable SaveTable(int? id, int number, bool isActive);
    bool DeleteTable(int id);
}

public sealed record ProductDto(int Id, string Name, string Description, string ImageUrl, decimal Price, IReadOnlyList<ProductExtraDto> Extras);
public sealed record ProductExtraDto(int Id, string Name, decimal Price);
public sealed record MenuCategoryDto(int Id, string Name, IReadOnlyList<ProductDto> Products);
public sealed record OrderLineRequest(int ProductId, int Quantity, string? Note = null, IReadOnlyList<int>? ExtraIds = null);
public sealed record TableAccountDto(
    int TableNumber,
    IReadOnlyList<Order> Orders,
    IReadOnlyList<TableAccountLineDto> Lines,
    decimal Total,
    decimal PaidTotal,
    decimal RemainingTotal);
public sealed record TableAccountLineDto(int ProductId, string ProductName, decimal UnitPrice, int Quantity);
public sealed record PaymentLineRequest(int ProductId, decimal UnitPrice, int Quantity);

public sealed class MenuService(IRestaurantRepository repository) : IMenuService
{
    public RestaurantTable? GetTable(int tableNumber) => repository.GetTable(tableNumber);

    public IReadOnlyList<MenuCategoryDto> GetMenu()
    {
        var products = repository.GetProducts()
            .Where(product => product.IsAvailable)
            .ToList();

        return repository.GetCategories()
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new MenuCategoryDto(
                category.Id,
                category.Name,
                products
                    .Where(product => product.CategoryId == category.Id)
                    .Select(ToDto)
                    .ToList()))
            .Where(category => category.Products.Count > 0)
            .ToList();
    }

    public ProductDto? GetProduct(int productId)
    {
        var product = repository.GetProduct(productId);
        return product is { IsAvailable: true } ? ToDto(product) : null;
    }

    private ProductDto ToDto(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.Price,
            repository.GetProductExtras(product.Id)
                .Select(extra => new ProductExtraDto(extra.Id, extra.Name, extra.Price))
                .ToList());
}

public sealed class OrderService(IRestaurantRepository repository) : IOrderService
{
    public Order Create(int tableNumber, IEnumerable<OrderLineRequest> lines)
    {
        if (repository.GetTable(tableNumber) is not { IsActive: true })
        {
            throw new InvalidOperationException("Masa bulunamadi.");
        }

        var items = lines
            .Where(line => line.Quantity > 0)
            .GroupBy(line => line.ProductId)
            .Select(group =>
            {
                var product = repository.GetProduct(group.Key);
                if (product is not { IsAvailable: true })
                {
                    throw new InvalidOperationException("Sepetteki urunlerden biri artik mevcut degil.");
                }

                var requestedExtraIds = group
                    .SelectMany(line => line.ExtraIds ?? [])
                    .Distinct()
                    .ToList();
                var extras = repository.GetProductExtras(product.Id)
                    .Where(extra => requestedExtraIds.Contains(extra.Id))
                    .ToList();

                return new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price + extras.Sum(extra => extra.Price),
                    Quantity = group.Sum(line => line.Quantity),
                    Note = group.Select(line => line.Note?.Trim()).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)) ?? string.Empty,
                    ExtrasSummary = string.Join(", ", extras.Select(extra => extra.Name))
                };
            })
            .ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Sepet bos.");
        }

        return repository.AddOrder(new Order
        {
            TableNumber = tableNumber,
            CreatedAt = DateTime.Now,
            Items = items
        });
    }

    public IReadOnlyList<Order> GetOrders() => repository.GetOrders();

    public bool UpdateStatus(int orderId, OrderStatus status) =>
        repository.UpdateOrderStatus(orderId, status);
}

public sealed class AdminService(IRestaurantRepository repository) : IAdminService
{
    public IReadOnlyList<Category> GetCategories() => repository.GetCategories();
    public Category? GetCategory(int id) => repository.GetCategory(id);

    public Category SaveCategory(int? id, string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Kategori adi zorunludur.");
        }

        var category = id.HasValue
            ? repository.GetCategory(id.Value) ?? throw new InvalidOperationException("Kategori bulunamadi.")
            : new Category { Name = string.Empty };

        category.Name = name.Trim();
        category.DisplayOrder = displayOrder;

        if (!id.HasValue)
        {
            return repository.AddCategory(category);
        }

        repository.UpdateCategory(category);
        return category;
    }

    public bool DeleteCategory(int id) => repository.DeleteCategory(id);
    public IReadOnlyList<Product> GetProducts() => repository.GetProducts();
    public Product? GetProduct(int id) => repository.GetProduct(id);

    public Product SaveProduct(int? id, int categoryId, string name, string description, string imageUrl, decimal price, bool isAvailable)
    {
        if (repository.GetCategory(categoryId) is null)
        {
            throw new InvalidOperationException("Kategori bulunamadi.");
        }

        if (string.IsNullOrWhiteSpace(name) || price < 0)
        {
            throw new InvalidOperationException("Urun adi ve gecerli fiyat zorunludur.");
        }

        var product = id.HasValue
            ? repository.GetProduct(id.Value) ?? throw new InvalidOperationException("Urun bulunamadi.")
            : new Product { Name = string.Empty, Description = string.Empty };

        product.CategoryId = categoryId;
        product.Name = name.Trim();
        product.Description = description?.Trim() ?? string.Empty;
        product.ImageUrl = imageUrl?.Trim() ?? string.Empty;
        product.Price = price;
        product.IsAvailable = isAvailable;

        if (!id.HasValue)
        {
            return repository.AddProduct(product);
        }

        repository.UpdateProduct(product);
        return product;
    }

    public bool DeleteProduct(int id) => repository.DeleteProduct(id);
    public IReadOnlyList<RestaurantTable> GetTables() => repository.GetTables();
    public RestaurantTable? GetTable(int id) => repository.GetTableById(id);

    public RestaurantTable SaveTable(int? id, int number, bool isActive)
    {
        if (number <= 0)
        {
            throw new InvalidOperationException("Masa numarasi sifirdan buyuk olmalidir.");
        }

        var duplicate = repository.GetTable(number);
        if (duplicate is not null && duplicate.Id != id)
        {
            throw new InvalidOperationException("Bu masa numarasi zaten kullaniliyor.");
        }

        var table = id.HasValue
            ? repository.GetTableById(id.Value) ?? throw new InvalidOperationException("Masa bulunamadi.")
            : new RestaurantTable();

        table.Number = number;
        table.IsActive = isActive;

        if (!id.HasValue)
        {
            return repository.AddTable(table);
        }

        repository.UpdateTable(table);
        return table;
    }

    public bool DeleteTable(int id) => repository.DeleteTable(id);
}

public sealed class OperationService(IRestaurantRepository repository) : IOperationService
{
    public IReadOnlyList<Order> GetKitchenOrders() =>
        repository.GetOrders()
            .Where(order => order.Status is OrderStatus.New or OrderStatus.Preparing or OrderStatus.Ready)
            .OrderBy(order => order.CreatedAt)
            .ToList();

    public IReadOnlyList<Order> GetReadyOrders() =>
        repository.GetOrders()
            .Where(order => order.Status == OrderStatus.Ready)
            .OrderBy(order => order.CreatedAt)
            .ToList();

    public IReadOnlyList<RestaurantTable> GetTables() => repository.GetTables();

    public bool AdvanceKitchenOrder(int orderId)
    {
        var order = repository.GetOrders().SingleOrDefault(item => item.Id == orderId);
        return order?.Status switch
        {
            OrderStatus.New => repository.UpdateOrderStatus(orderId, OrderStatus.Preparing),
            OrderStatus.Preparing => repository.UpdateOrderStatus(orderId, OrderStatus.Ready),
            _ => false
        };
    }

    public bool DeliverOrder(int orderId)
    {
        var order = repository.GetOrders().SingleOrDefault(item => item.Id == orderId);
        return order?.Status == OrderStatus.Ready &&
               repository.UpdateOrderStatus(orderId, OrderStatus.Delivered);
    }

    public IReadOnlyList<TableServiceRequest> GetActiveServiceRequests() =>
        repository.GetActiveServiceRequests();

    public TableServiceRequest CreateServiceRequest(int tableNumber, ServiceRequestType type)
    {
        if (repository.GetTable(tableNumber) is not { IsActive: true })
        {
            throw new InvalidOperationException("Masa bulunamadı.");
        }

        return repository.GetActiveServiceRequests()
            .FirstOrDefault(request => request.TableNumber == tableNumber && request.Type == type)
            ?? repository.AddServiceRequest(new TableServiceRequest
            {
                TableNumber = tableNumber,
                Type = type,
                CreatedAt = DateTime.Now
            });
    }

    public bool ResolveServiceRequest(int id) => repository.ResolveServiceRequest(id);

    public IReadOnlyList<TableAccountDto> GetOpenTableAccounts() =>
        repository.GetOrders()
            .Where(order => order.Status == OrderStatus.Delivered)
            .GroupBy(order => order.TableNumber)
            .Select(group =>
            {
                var orders = group.OrderBy(order => order.CreatedAt).ToList();
                var total = orders.Sum(order => order.Total);
                var payments = repository.GetPayments()
                    .Where(payment => payment.TableNumber == group.Key)
                    .ToList();
                var paidTotal = payments
                    .Sum(payment => payment.Amount);
                var paidQuantities = payments
                    .SelectMany(payment => payment.Items)
                    .GroupBy(item => (item.ProductId, item.UnitPrice))
                    .ToDictionary(line => line.Key, line => line.Sum(item => item.Quantity));

                return new TableAccountDto(
                    group.Key,
                    orders,
                    orders
                        .SelectMany(order => order.Items)
                        .GroupBy(item => new { item.ProductId, item.ProductName, item.UnitPrice })
                        .Select(line => new TableAccountLineDto(
                            line.Key.ProductId,
                            line.Key.ProductName,
                            line.Key.UnitPrice,
                            line.Sum(item => item.Quantity) - paidQuantities.GetValueOrDefault((line.Key.ProductId, line.Key.UnitPrice))))
                        .Where(line => line.Quantity > 0)
                        .OrderBy(line => line.ProductName)
                        .ToList(),
                    total,
                    paidTotal,
                    total - paidTotal);
            })
            .Where(account => account.RemainingTotal > 0)
            .OrderBy(account => account.TableNumber)
            .ToList();

    public bool CollectTablePayment(
        int tableNumber,
        decimal amount,
        PaymentMethod method,
        IReadOnlyList<PaymentLineRequest>? lines = null) =>
        repository.CollectTablePayment(tableNumber, amount, method, lines);
}

public sealed class BranchService(IRestaurantRepository repository, IBranchContext branchContext) : IBranchService
{
    public IReadOnlyList<Branch> GetBranches() => repository.GetBranches();

    public Branch? GetBranch(int id) => repository.GetBranch(id);

    public Branch Save(int? id, string name, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Şube adı zorunludur.");
        }

        var branch = id.HasValue
            ? repository.GetBranch(id.Value) ?? throw new InvalidOperationException("Şube bulunamadı.")
            : new Branch { Name = string.Empty };

        if (id.HasValue && branch.IsActive && !isActive)
        {
            if (branch.Id == branchContext.BranchId)
            {
                throw new InvalidOperationException("Seçili şube pasife alınamaz. Önce başka bir şubeye geçin.");
            }

            if (!repository.GetBranches().Any(other => other.Id != branch.Id && other.IsActive))
            {
                throw new InvalidOperationException("Son aktif şube pasife alınamaz.");
            }
        }

        branch.Name = name.Trim();
        branch.IsActive = isActive;

        if (!id.HasValue)
        {
            return repository.AddBranch(branch);
        }

        repository.UpdateBranch(branch);
        return branch;
    }
}
