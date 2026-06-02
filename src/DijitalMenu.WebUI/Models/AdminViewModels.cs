using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record AdminCatalogViewModel(
    IReadOnlyList<Category> Categories,
    IReadOnlyList<Product> Products,
    CategoryInput Category);

public sealed class CategoryInput
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public sealed record ProductFormViewModel(IReadOnlyList<Category> Categories, ProductInput Product);

public sealed class ProductInput
{
    public int? Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public sealed record AdminTablesViewModel(IReadOnlyList<RestaurantTable> Tables, TableInput Table);

public sealed class TableInput
{
    public int? Id { get; set; }
    public int Number { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record TableQrViewModel(int TableNumber, string MenuUrl, string Svg);
