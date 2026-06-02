using System.ComponentModel.DataAnnotations;

namespace DijitalMenu.WebUI.Models;

public sealed class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public IReadOnlyList<LoginStaffOptionViewModel> StaffOptions { get; set; } = [];
}

public sealed record LoginStaffOptionViewModel(string Username, string DisplayName, string Role);
