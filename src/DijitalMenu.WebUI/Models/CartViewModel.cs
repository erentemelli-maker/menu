namespace DijitalMenu.WebUI.Models;

public sealed record CartItemSession(int ProductId, int Quantity, string Note = "", IReadOnlyList<int>? ExtraIds = null);

public sealed record CartLineViewModel(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    string Note,
    IReadOnlyList<CartExtraViewModel> Extras)
{
    public decimal ExtrasTotal => Extras.Where(extra => extra.IsSelected).Sum(extra => extra.Price);
    public decimal LineTotal => (UnitPrice + ExtrasTotal) * Quantity;
}

public sealed record CartExtraViewModel(int Id, string Name, decimal Price, bool IsSelected);

public sealed record CartViewModel(int BranchId, int TableNumber, IReadOnlyList<CartLineViewModel> Lines)
{
    public decimal Total => Lines.Sum(line => line.LineTotal);
}
