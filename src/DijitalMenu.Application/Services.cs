using DijitalMenu.Domain;

namespace DijitalMenu.Application;

public interface IRestaurantRepository
{
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
    IReadOnlyList<Order> GetOrders();
    Order AddOrder(Order order);
    bool UpdateOrderStatus(int orderId, OrderStatus status);
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
}

public interface IAdminService
{
    IReadOnlyList<Category> GetCategories();
    Category? GetCategory(int id);
    Category SaveCategory(int? id, string name, int displayOrder);
    bool DeleteCategory(int id);
    IReadOnlyList<Product> GetProducts();
    Product? GetProduct(int id);
    Product SaveProduct(int? id, int categoryId, string name, string description, decimal price, bool isAvailable);
    bool DeleteProduct(int id);
    IReadOnlyList<RestaurantTable> GetTables();
    RestaurantTable? GetTable(int id);
    RestaurantTable SaveTable(int? id, int number, bool isActive);
    bool DeleteTable(int id);
}

public sealed record ProductDto(int Id, string Name, string Description, decimal Price);
public sealed record MenuCategoryDto(int Id, string Name, IReadOnlyList<ProductDto> Products);
public sealed record OrderLineRequest(int ProductId, int Quantity);

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

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price);
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

                return new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = group.Sum(line => line.Quantity)
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

    public Product SaveProduct(int? id, int categoryId, string name, string description, decimal price, bool isAvailable)
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
}
