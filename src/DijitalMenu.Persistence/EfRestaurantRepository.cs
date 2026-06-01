using DijitalMenu.Application;
using DijitalMenu.Domain;
using Microsoft.EntityFrameworkCore;

namespace DijitalMenu.Persistence;

public sealed class EfRestaurantRepository(RestaurantDbContext dbContext) : IRestaurantRepository
{
    public RestaurantTable? GetTable(int tableNumber) =>
        dbContext.Tables.AsNoTracking().SingleOrDefault(table => table.Number == tableNumber);

    public IReadOnlyList<RestaurantTable> GetTables() =>
        dbContext.Tables.AsNoTracking().OrderBy(table => table.Number).ToList();

    public RestaurantTable? GetTableById(int id) => dbContext.Tables.Find(id);

    public RestaurantTable AddTable(RestaurantTable table)
    {
        dbContext.Tables.Add(table);
        dbContext.SaveChanges();
        return table;
    }

    public bool UpdateTable(RestaurantTable table) => SaveExisting(table);

    public bool DeleteTable(int id) => Delete(dbContext.Tables, id);

    public IReadOnlyList<Category> GetCategories() =>
        dbContext.Categories.AsNoTracking().OrderBy(category => category.DisplayOrder).ToList();

    public Category? GetCategory(int id) => dbContext.Categories.Find(id);

    public Category AddCategory(Category category)
    {
        dbContext.Categories.Add(category);
        dbContext.SaveChanges();
        return category;
    }

    public bool UpdateCategory(Category category) => SaveExisting(category);

    public bool DeleteCategory(int id)
    {
        if (dbContext.Products.Any(product => product.CategoryId == id))
        {
            return false;
        }

        return Delete(dbContext.Categories, id);
    }

    public IReadOnlyList<Product> GetProducts() =>
        dbContext.Products.AsNoTracking().OrderBy(product => product.Name).ToList();

    public Product? GetProduct(int productId) => dbContext.Products.Find(productId);

    public Product AddProduct(Product product)
    {
        dbContext.Products.Add(product);
        dbContext.SaveChanges();
        return product;
    }

    public bool UpdateProduct(Product product) => SaveExisting(product);

    public bool DeleteProduct(int id) => Delete(dbContext.Products, id);

    public IReadOnlyList<Order> GetOrders() =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

    public Order AddOrder(Order order)
    {
        dbContext.Orders.Add(order);
        var table = dbContext.Tables.Single(item => item.Number == order.TableNumber);
        table.Status = TableStatus.Occupied;
        dbContext.SaveChanges();
        return order;
    }

    public bool UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = dbContext.Orders.Find(orderId);
        if (order is null)
        {
            return false;
        }

        order.Status = status;
        var table = dbContext.Tables.SingleOrDefault(item => item.Number == order.TableNumber);
        if (table is not null)
        {
            table.Status = status switch
            {
                OrderStatus.Ready => TableStatus.ServiceWaiting,
                OrderStatus.Delivered when !dbContext.Orders.Any(item =>
                    item.TableNumber == order.TableNumber &&
                    item.Id != order.Id &&
                    item.Status != OrderStatus.Delivered) => TableStatus.Available,
                OrderStatus.Delivered => TableStatus.Occupied,
                _ => TableStatus.Occupied
            };
        }

        dbContext.SaveChanges();
        return true;
    }

    private bool SaveExisting<TEntity>(TEntity entity) where TEntity : class
    {
        dbContext.Update(entity);
        return dbContext.SaveChanges() > 0;
    }

    private bool Delete<TEntity>(DbSet<TEntity> set, int id) where TEntity : class
    {
        var entity = set.Find(id);
        if (entity is null)
        {
            return false;
        }

        set.Remove(entity);
        dbContext.SaveChanges();
        return true;
    }
}
