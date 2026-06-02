using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Presentation;

public static class StaffRolePresentation
{
    public static string ToTurkish(this StaffRole role) => role switch
    {
        StaffRole.Admin => "Yönetici",
        StaffRole.Waiter => "Garson",
        StaffRole.Kitchen => "Mutfak",
        _ => role.ToString()
    };
}
