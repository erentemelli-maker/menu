using System.ComponentModel.DataAnnotations;
using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Models;

public sealed record AdminStaffViewModel(IReadOnlyList<StaffUser> Users, StaffInput Staff);

public sealed class StaffInput
{
    public int? Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public StaffRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
