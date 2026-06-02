using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record AdminBranchesViewModel(IReadOnlyList<Branch> Branches, BranchInput Branch);

public sealed class BranchInput
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
