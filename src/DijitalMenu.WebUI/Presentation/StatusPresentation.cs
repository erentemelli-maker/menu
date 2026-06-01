using DijitalMenu.Domain;

namespace DijitalMenu.WebUI.Presentation;

public static class StatusPresentation
{
    public static string ToTurkish(this OrderStatus status) => status switch
    {
        OrderStatus.New => "Yeni",
        OrderStatus.Preparing => "Hazırlanıyor",
        OrderStatus.Ready => "Hazır",
        OrderStatus.Delivered => "Teslim Edildi",
        _ => status.ToString()
    };

    public static string ToTurkish(this TableStatus status) => status switch
    {
        TableStatus.Available => "Boş",
        TableStatus.Occupied => "Dolu",
        TableStatus.ServiceWaiting => "Servis Bekliyor",
        _ => status.ToString()
    };
}
