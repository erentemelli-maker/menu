using DijitalMenu.Application;

namespace DijitalMenu.WebUI.Models;

public sealed record MenuViewModel(int TableNumber, IReadOnlyList<MenuCategoryDto> Categories);
