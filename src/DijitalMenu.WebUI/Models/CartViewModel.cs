namespace DijitalMenu.WebUI.Models;

public sealed record CartItemSession(int ProductId, int Quantity);

public sealed record CartLineViewModel(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartViewModel(int TableNumber, IReadOnlyList<CartLineViewModel> Lines)
{
    public decimal Total => Lines.Sum(line => line.LineTotal);
}
