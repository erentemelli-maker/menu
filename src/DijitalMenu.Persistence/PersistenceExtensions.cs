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
            IF COL_LENGTH('Tables', 'Status') IS NULL
            BEGIN
                ALTER TABLE [Tables] ADD [Status] int NOT NULL CONSTRAINT [DF_Tables_Status] DEFAULT 0;
            END
            """);

        if (dbContext.Tables.Any())
        {
            return;
        }

        dbContext.Tables.AddRange(Enumerable.Range(1, 5).Select(number =>
            new RestaurantTable { Number = number }));

        var drinks = new Category { Name = "Icecekler", DisplayOrder = 1 };
        var burgers = new Category { Name = "Burgerler", DisplayOrder = 2 };
        var salads = new Category { Name = "Salatalar", DisplayOrder = 3 };
        var desserts = new Category { Name = "Tatlilar", DisplayOrder = 4 };

        dbContext.Categories.AddRange(drinks, burgers, salads, desserts);
        dbContext.SaveChanges();

        dbContext.Products.AddRange(
            new Product { CategoryId = drinks.Id, Name = "Cola", Description = "330 ml soguk icecek", Price = 55 },
            new Product { CategoryId = drinks.Id, Name = "Ayran", Description = "Ev yapimi ayran", Price = 35 },
            new Product { CategoryId = drinks.Id, Name = "Filtre Kahve", Description = "Taze cekilmis kahve", Price = 75 },
            new Product { CategoryId = burgers.Id, Name = "Klasik Burger", Description = "Dana kofte, cheddar, marul ve ozel sos", Price = 245 },
            new Product { CategoryId = burgers.Id, Name = "Tavuk Burger", Description = "Citir tavuk, marul ve ranch sos", Price = 215 },
            new Product { CategoryId = salads.Id, Name = "Sezar Salata", Description = "Izgara tavuk, parmesan ve kruton", Price = 190 },
            new Product { CategoryId = desserts.Id, Name = "San Sebastian", Description = "Gunluk hazirlanan cheesecake", Price = 160 });

        dbContext.SaveChanges();
    }
}
