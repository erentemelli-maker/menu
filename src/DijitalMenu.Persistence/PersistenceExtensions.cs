using DijitalMenu.Application;
using DijitalMenu.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DijitalMenu.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<RestaurantDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IRestaurantRepository, EfRestaurantRepository>();
        return services;
    }

    public static void InitializePersistence(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[Branches]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Branches] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(120) NOT NULL,
                    [IsActive] bit NOT NULL,
                    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
                );
                SET IDENTITY_INSERT [Branches] ON;
                INSERT INTO [Branches] ([Id], [Name], [IsActive]) VALUES (1, N'Merkez Şube', CAST(1 AS bit));
                SET IDENTITY_INSERT [Branches] OFF;
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('Tables', 'Status') IS NULL
            BEGIN
                ALTER TABLE [Tables] ADD [Status] int NOT NULL CONSTRAINT [DF_Tables_Status] DEFAULT 0;
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('Tables', 'BranchId') IS NULL
                ALTER TABLE [Tables] ADD [BranchId] int NOT NULL CONSTRAINT [DF_Tables_BranchId] DEFAULT 1;
            IF COL_LENGTH('Products', 'BranchId') IS NULL
                ALTER TABLE [Products] ADD [BranchId] int NOT NULL CONSTRAINT [DF_Products_BranchId] DEFAULT 1;
            IF COL_LENGTH('Products', 'ImageUrl') IS NULL
                ALTER TABLE [Products] ADD [ImageUrl] nvarchar(600) NOT NULL CONSTRAINT [DF_Products_ImageUrl] DEFAULT '';
            IF COL_LENGTH('Orders', 'BranchId') IS NULL
                ALTER TABLE [Orders] ADD [BranchId] int NOT NULL CONSTRAINT [DF_Orders_BranchId] DEFAULT 1;
            IF COL_LENGTH('Orders', 'IsPaid') IS NULL
                ALTER TABLE [Orders] ADD [IsPaid] bit NOT NULL CONSTRAINT [DF_Orders_IsPaid] DEFAULT 1;
            IF COL_LENGTH('OrderItems', 'Note') IS NULL
                ALTER TABLE [OrderItems] ADD [Note] nvarchar(max) NOT NULL CONSTRAINT [DF_OrderItems_Note] DEFAULT '';
            IF COL_LENGTH('OrderItems', 'ExtrasSummary') IS NULL
                ALTER TABLE [OrderItems] ADD [ExtrasSummary] nvarchar(max) NOT NULL CONSTRAINT [DF_OrderItems_ExtrasSummary] DEFAULT '';
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[ProductExtras]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ProductExtras] (
                    [Id] int NOT NULL IDENTITY,
                    [ProductId] int NOT NULL,
                    [Name] nvarchar(max) NOT NULL,
                    [Price] decimal(18,2) NOT NULL,
                    CONSTRAINT [PK_ProductExtras] PRIMARY KEY ([Id])
                );
            END
            IF OBJECT_ID(N'[TableServiceRequests]', N'U') IS NULL
            BEGIN
                CREATE TABLE [TableServiceRequests] (
                    [Id] int NOT NULL IDENTITY,
                    [BranchId] int NOT NULL,
                    [TableNumber] int NOT NULL,
                    [Type] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [IsResolved] bit NOT NULL,
                    CONSTRAINT [PK_TableServiceRequests] PRIMARY KEY ([Id])
                );
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[Payments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Payments] (
                    [Id] int NOT NULL IDENTITY,
                    [BranchId] int NOT NULL,
                    [TableNumber] int NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Method] int NOT NULL,
                    [PaidAt] datetime2 NOT NULL,
                    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
                );
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[PaymentItems]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PaymentItems] (
                    [Id] int NOT NULL IDENTITY,
                    [PaymentId] int NOT NULL,
                    [ProductId] int NOT NULL,
                    [ProductName] nvarchar(max) NOT NULL,
                    [UnitPrice] decimal(18,2) NOT NULL,
                    [Quantity] int NOT NULL,
                    CONSTRAINT [PK_PaymentItems] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PaymentItems_Payments_PaymentId] FOREIGN KEY ([PaymentId]) REFERENCES [Payments] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_PaymentItems_PaymentId] ON [PaymentItems] ([PaymentId]);
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            UPDATE [Tables]
            SET [Status] = 0
            WHERE NOT EXISTS (
                SELECT 1
                FROM [Orders]
                WHERE [Orders].[BranchId] = [Tables].[BranchId]
                  AND [Orders].[TableNumber] = [Tables].[Number]
                  AND ([Orders].[Status] <> 3 OR [Orders].[IsPaid] = 0)
            );
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'[StaffUsers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [StaffUsers] (
                    [Id] int NOT NULL IDENTITY,
                    [BranchId] int NOT NULL,
                    [Username] nvarchar(64) NOT NULL,
                    [DisplayName] nvarchar(120) NOT NULL,
                    [PasswordHash] nvarchar(512) NOT NULL,
                    [Role] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    CONSTRAINT [PK_StaffUsers] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_StaffUsers_Username] ON [StaffUsers] ([Username]);
            END
            """);
        dbContext.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('StaffUsers', 'BranchId') IS NULL
                ALTER TABLE [StaffUsers] ADD [BranchId] int NOT NULL CONSTRAINT [DF_StaffUsers_BranchId] DEFAULT 1;

            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tables_Number' AND object_id = OBJECT_ID(N'[Tables]'))
                DROP INDEX [IX_Tables_Number] ON [Tables];
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tables_BranchId_Number' AND object_id = OBJECT_ID(N'[Tables]'))
                CREATE UNIQUE INDEX [IX_Tables_BranchId_Number] ON [Tables] ([BranchId], [Number]);
            """);

        if (!dbContext.StaffUsers.Any())
        {
            var passwordService = new PasswordService();
            dbContext.StaffUsers.AddRange(
                new StaffUser
                {
                    Username = "admin",
                    BranchId = 1,
                    DisplayName = "İşletme Yöneticisi",
                    Role = StaffRole.Admin,
                    PasswordHash = passwordService.Hash("4572")
                },
                new StaffUser
                {
                    Username = "garson",
                    BranchId = 1,
                    DisplayName = "Servis Ekibi",
                    Role = StaffRole.Waiter,
                    PasswordHash = passwordService.Hash("4572")
                },
                new StaffUser
                {
                    Username = "mutfak",
                    BranchId = 1,
                    DisplayName = "Mutfak Ekibi",
                    Role = StaffRole.Kitchen,
                    PasswordHash = passwordService.Hash("4572")
                });
            dbContext.SaveChanges();
        }

        ResetLegacyDemoPasswords(dbContext);

        if (!dbContext.Tables.Any())
        {
            dbContext.Tables.AddRange(Enumerable.Range(1, 5).Select(number =>
                new RestaurantTable { BranchId = 1, Number = number }));

            var drinks = new Category { Name = "Icecekler", DisplayOrder = 1 };
            var burgers = new Category { Name = "Burgerler", DisplayOrder = 2 };
            var salads = new Category { Name = "Salatalar", DisplayOrder = 3 };
            var desserts = new Category { Name = "Tatlilar", DisplayOrder = 4 };

            dbContext.Categories.AddRange(drinks, burgers, salads, desserts);
            dbContext.SaveChanges();
        }

        AddMenuShowcase(dbContext);
        AddProductExtras(dbContext);
        dbContext.SaveChanges();
    }

    private static void ResetLegacyDemoPasswords(RestaurantDbContext dbContext)
    {
        var passwordService = new PasswordService();
        var users = dbContext.StaffUsers.ToList();
        var legacyPasswords = new[] { "admin123", "garson123", "mutfak123" };
        if (!users.Any(user => legacyPasswords.Any(password =>
                passwordService.Verify(password, user.PasswordHash))))
        {
            return;
        }

        foreach (var user in users)
        {
            user.PasswordHash = passwordService.Hash("4572");
        }

        dbContext.SaveChanges();
    }

    private static void AddMenuShowcase(RestaurantDbContext dbContext)
    {
        var drinks = dbContext.Categories.First(category => category.DisplayOrder == 1);
        var burgers = dbContext.Categories.First(category => category.DisplayOrder == 2);
        var salads = dbContext.Categories.First(category => category.DisplayOrder == 3);
        var desserts = dbContext.Categories.First(category => category.DisplayOrder == 4);

        var drinkImage = "https://images.unsplash.com/photo-1544145945-f90425340c7e?auto=format&fit=crop&w=720&q=80";
        var coffeeImage = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=720&q=80";
        var burgerImage = "https://images.unsplash.com/photo-1568901346375-23c9450c58cd?auto=format&fit=crop&w=720&q=80";
        var saladImage = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=720&q=80";
        var dessertImage = "https://images.unsplash.com/photo-1571877227200-a0d98ea607e9?auto=format&fit=crop&w=720&q=80";

        AddProduct(dbContext, drinks.Id, "Cola", "330 ml soğuk içecek", 55, drinkImage);
        AddProduct(dbContext, drinks.Id, "Ayran", "Ev yapımı ferah ayran", 35, drinkImage);
        AddProduct(dbContext, drinks.Id, "Filtre Kahve", "Taze çekilmiş çekirdek kahve", 75, coffeeImage);
        AddProduct(dbContext, drinks.Id, "Limonata", "Taze limon ve nane ile", 70, drinkImage);
        AddProduct(dbContext, drinks.Id, "Şeftalili Soğuk Çay", "Buz gibi meyveli ferahlık", 65, drinkImage);
        AddProduct(dbContext, drinks.Id, "Latte", "Espresso ve kadifemsi süt", 95, coffeeImage);
        AddProduct(dbContext, drinks.Id, "Türk Kahvesi", "Geleneksel sunumuyla", 70, coffeeImage);
        AddProduct(dbContext, drinks.Id, "Maden Suyu", "Doğal mineralli sade soda", 35, drinkImage);

        AddProduct(dbContext, burgers.Id, "Klasik Burger", "Dana köfte, cheddar, marul ve özel sos", 245, burgerImage);
        AddProduct(dbContext, burgers.Id, "Tavuk Burger", "Çıtır tavuk, marul ve ranch sos", 215, burgerImage);
        AddProduct(dbContext, burgers.Id, "Double Smash Burger", "İki dana köfte, karamelize soğan ve cheddar", 320, burgerImage);
        AddProduct(dbContext, burgers.Id, "Mantar Soslu Burger", "Dana köfte, mantar sos ve roka", 285, burgerImage);
        AddProduct(dbContext, burgers.Id, "Acılı Burger", "Dana köfte, jalapeno ve acılı mayo", 275, burgerImage);
        AddProduct(dbContext, burgers.Id, "Veggie Burger", "Sebze köftesi, avokado ve yeşillik", 230, burgerImage);

        AddProduct(dbContext, salads.Id, "Sezar Salata", "Izgara tavuk, parmesan ve kruton", 190, saladImage);
        AddProduct(dbContext, salads.Id, "Akdeniz Salata", "Domates, salatalık, zeytin ve beyaz peynir", 170, saladImage);
        AddProduct(dbContext, salads.Id, "Izgara Tavuklu Salata", "Mevsim yeşillikleri ve ızgara tavuk", 220, saladImage);
        AddProduct(dbContext, salads.Id, "Ton Balıklı Salata", "Ton balığı, mısır ve zeytinyağlı sos", 235, saladImage);

        AddProduct(dbContext, desserts.Id, "San Sebastian", "Günlük hazırlanan cheesecake", 160, dessertImage);
        AddProduct(dbContext, desserts.Id, "Çikolatalı Sufle", "Akışkan çikolata ve vanilyalı dondurma", 175, dessertImage);
        AddProduct(dbContext, desserts.Id, "Tiramisu", "Kahve aromalı klasik İtalyan tatlısı", 155, dessertImage);
        AddProduct(dbContext, desserts.Id, "Magnolia", "Muz, bisküvi ve hafif krema", 145, dessertImage);
    }

    private static void AddProduct(
        RestaurantDbContext dbContext,
        int categoryId,
        string name,
        string description,
        decimal price,
        string imageUrl)
    {
        var product = dbContext.Products.FirstOrDefault(item =>
            item.BranchId == 1 && item.Name == name);
        if (product is null)
        {
            dbContext.Products.Add(new Product
            {
                BranchId = 1,
                CategoryId = categoryId,
                Name = name,
                Description = description,
                Price = price,
                ImageUrl = imageUrl
            });
            return;
        }

        product.ImageUrl = imageUrl;
    }

    private static void AddProductExtras(RestaurantDbContext dbContext)
    {
        foreach (var product in dbContext.Products.Where(product => product.BranchId == 1).ToList())
        {
            if (product.Name.Contains("Burger", StringComparison.OrdinalIgnoreCase))
            {
                AddExtra(dbContext, product.Id, "Ekstra cheddar", 25);
                AddExtra(dbContext, product.Id, "Çift köfte", 85);
                AddExtra(dbContext, product.Id, "Ekstra sos", 15);
            }
            else if (product.CategoryId == dbContext.Categories.First(category => category.DisplayOrder == 1).Id)
            {
                AddExtra(dbContext, product.Id, "Büyük boy", 20);
            }
        }
    }

    private static void AddExtra(RestaurantDbContext dbContext, int productId, string name, decimal price)
    {
        if (!dbContext.ProductExtras.Any(extra => extra.ProductId == productId && extra.Name == name))
        {
            dbContext.ProductExtras.Add(new ProductExtra { ProductId = productId, Name = name, Price = price });
        }
    }
}
