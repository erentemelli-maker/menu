using DijitalMenu.Application;
using DijitalMenu.Domain;
using Microsoft.EntityFrameworkCore;

namespace DijitalMenu.Persistence;

public sealed class EfRestaurantRepository(RestaurantDbContext dbContext, IBranchContext branchContext) : IRestaurantRepository
{
    public IReadOnlyList<Branch> GetBranches() =>
        dbContext.Branches.AsNoTracking().OrderBy(branch => branch.Name).ToList();

    public Branch? GetBranch(int id) => dbContext.Branches.Find(id);

    public Branch AddBranch(Branch branch)
    {
        dbContext.Branches.Add(branch);
        dbContext.SaveChanges();
        return branch;
    }

    public bool UpdateBranch(Branch branch) => SaveExisting(branch);

    public RestaurantTable? GetTable(int tableNumber) =>
        dbContext.Tables.AsNoTracking().SingleOrDefault(table =>
            table.BranchId == branchContext.BranchId && table.Number == tableNumber);

    public IReadOnlyList<RestaurantTable> GetTables() =>
        dbContext.Tables.AsNoTracking()
            .Where(table => table.BranchId == branchContext.BranchId)
            .OrderBy(table => table.Number)
            .ToList();

    public RestaurantTable? GetTableById(int id) =>
        dbContext.Tables.SingleOrDefault(table => table.Id == id && table.BranchId == branchContext.BranchId);

    public RestaurantTable AddTable(RestaurantTable table)
    {
        table.BranchId = branchContext.BranchId;
        dbContext.Tables.Add(table);
        dbContext.SaveChanges();
        return table;
    }

    public bool UpdateTable(RestaurantTable table) => SaveExisting(table);

    public bool DeleteTable(int id) =>
        GetTableById(id) is { } table && DeleteEntity(table);

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
        dbContext.Products.AsNoTracking()
            .Where(product => product.BranchId == branchContext.BranchId)
            .OrderBy(product => product.Name)
            .ToList();

    public Product? GetProduct(int productId) =>
        dbContext.Products.SingleOrDefault(product =>
            product.Id == productId && product.BranchId == branchContext.BranchId);

    public Product AddProduct(Product product)
    {
        product.BranchId = branchContext.BranchId;
        dbContext.Products.Add(product);
        dbContext.SaveChanges();
        return product;
    }

    public bool UpdateProduct(Product product) => SaveExisting(product);

    public bool DeleteProduct(int id) =>
        GetProduct(id) is { } product && DeleteEntity(product);

    public IReadOnlyList<ProductExtra> GetProductExtras(int productId) =>
        dbContext.ProductExtras.AsNoTracking()
            .Where(extra => extra.ProductId == productId)
            .OrderBy(extra => extra.Name)
            .ToList();

    public IReadOnlyList<Order> GetOrders() =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.BranchId == branchContext.BranchId)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

    public Order AddOrder(Order order)
    {
        order.BranchId = branchContext.BranchId;
        dbContext.Orders.Add(order);
        var table = dbContext.Tables.Single(item =>
            item.BranchId == branchContext.BranchId && item.Number == order.TableNumber);
        table.Status = TableStatus.Occupied;
        dbContext.SaveChanges();
        return order;
    }

    public bool UpdateOrderStatus(int orderId, OrderStatus status)
    {
        var order = dbContext.Orders.SingleOrDefault(item =>
            item.Id == orderId && item.BranchId == branchContext.BranchId);
        if (order is null)
        {
            return false;
        }

        if (order.IsPaid && status != OrderStatus.Delivered)
        {
            return false;
        }

        order.Status = status;
        var table = dbContext.Tables.SingleOrDefault(item =>
            item.BranchId == branchContext.BranchId && item.Number == order.TableNumber);
        if (table is not null)
        {
            table.Status = status switch
            {
                OrderStatus.Ready => TableStatus.ServiceWaiting,
                OrderStatus.Delivered => TableStatus.Occupied,
                _ => TableStatus.Occupied
            };
        }

        dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyList<Payment> GetPayments() =>
        dbContext.Payments.AsNoTracking()
            .Include(payment => payment.Items)
            .Where(payment => payment.BranchId == branchContext.BranchId)
            .OrderByDescending(payment => payment.PaidAt)
            .ToList();

    public bool CollectTablePayment(
        int tableNumber,
        decimal amount,
        PaymentMethod method,
        IReadOnlyList<PaymentLineRequest>? lines = null)
    {
        var orders = dbContext.Orders
            .Include(order => order.Items)
            .Where(order =>
                order.BranchId == branchContext.BranchId &&
                order.TableNumber == tableNumber)
            .ToList();

        if (orders.Count == 0 || orders.Any(order => order.Status != OrderStatus.Delivered))
        {
            return false;
        }

        var paidTotal = dbContext.Payments
            .Where(payment =>
                payment.BranchId == branchContext.BranchId &&
                payment.TableNumber == tableNumber)
            .Sum(payment => payment.Amount);
        var total = orders.Sum(order => order.Items.Sum(item => item.UnitPrice * item.Quantity));
        var remainingTotal = total - paidTotal;

        var selectedLines = lines?
            .Where(line => line.Quantity > 0)
            .GroupBy(line => (line.ProductId, line.UnitPrice))
            .Select(group => new PaymentLineRequest(
                group.Key.ProductId,
                group.Key.UnitPrice,
                group.Sum(line => line.Quantity)))
            .ToList() ?? [];
        var paidItems = dbContext.PaymentItems
            .Where(item => dbContext.Payments.Any(payment =>
                payment.Id == item.PaymentId &&
                payment.BranchId == branchContext.BranchId &&
                payment.TableNumber == tableNumber))
            .ToList()
            .GroupBy(item => (item.ProductId, item.UnitPrice))
        .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var orderedItems = orders
            .SelectMany(order => order.Items)
        .GroupBy(item => (item.ProductId, item.UnitPrice))
        .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var selectedTotal = selectedLines.Sum(line => line.UnitPrice * line.Quantity);

        if (amount <= 0 || amount > remainingTotal ||
            (selectedLines.Count > 0 && selectedTotal != amount) ||
            selectedLines.Any(line =>
                !orderedItems.TryGetValue((line.ProductId, line.UnitPrice), out var orderedQuantity) ||
                line.Quantity > orderedQuantity - paidItems.GetValueOrDefault((line.ProductId, line.UnitPrice))))
        {
            return false;
        }

        dbContext.Payments.Add(new Payment
        {
            BranchId = branchContext.BranchId,
            TableNumber = tableNumber,
            Amount = amount,
            Method = method,
            PaidAt = DateTime.Now,
            Items = selectedLines.Select(line => new PaymentItem
            {
                ProductId = line.ProductId,
                ProductName = orders
                    .SelectMany(order => order.Items)
                    .First(item => item.ProductId == line.ProductId)
                    .ProductName,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity
            }).ToList()
        });

        if (amount == remainingTotal)
        {
            foreach (var order in orders)
            {
                order.IsPaid = true;
            }

            var table = dbContext.Tables.SingleOrDefault(item =>
                item.BranchId == branchContext.BranchId && item.Number == tableNumber);
            if (table is not null)
            {
                table.Status = TableStatus.Available;
            }
        }

        dbContext.SaveChanges();
        return true;
    }

    public IReadOnlyList<StaffUser> GetStaffUsers() =>
        dbContext.StaffUsers.AsNoTracking()
            .Where(user => user.BranchId == branchContext.BranchId)
            .OrderBy(user => user.DisplayName)
            .ToList();

    public StaffUser? GetStaffUser(int id) =>
        dbContext.StaffUsers.SingleOrDefault(user =>
            user.Id == id && user.BranchId == branchContext.BranchId);

    public StaffUser? GetStaffUser(string username) =>
        dbContext.StaffUsers.SingleOrDefault(user => user.Username == username);

    public StaffUser AddStaffUser(StaffUser user)
    {
        user.BranchId = branchContext.BranchId;
        dbContext.StaffUsers.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    public bool UpdateStaffUser(StaffUser user) => SaveExisting(user);

    public IReadOnlyList<TableServiceRequest> GetActiveServiceRequests() =>
        dbContext.TableServiceRequests.AsNoTracking()
            .Where(request =>
                request.BranchId == branchContext.BranchId &&
                !request.IsResolved)
            .OrderBy(request => request.CreatedAt)
            .ToList();

    public TableServiceRequest AddServiceRequest(TableServiceRequest request)
    {
        request.BranchId = branchContext.BranchId;
        dbContext.TableServiceRequests.Add(request);
        dbContext.SaveChanges();
        return request;
    }

    public bool ResolveServiceRequest(int id)
    {
        var request = dbContext.TableServiceRequests.SingleOrDefault(item =>
            item.Id == id &&
            item.BranchId == branchContext.BranchId &&
            !item.IsResolved);
        if (request is null)
        {
            return false;
        }

        request.IsResolved = true;
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

    private bool DeleteEntity<TEntity>(TEntity entity) where TEntity : class
    {
        dbContext.Remove(entity);
        dbContext.SaveChanges();
        return true;
    }
}
