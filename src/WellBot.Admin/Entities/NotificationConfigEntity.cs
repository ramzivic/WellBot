using WellBot.Shared.Enums;

namespace WellBot.Admin.Entities;

public class NotificationConfigEntity
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AnimationName { get; set; } = string.Empty;

    /// <summary>Intervalle en minutes.</summary>
    public int IntervalMinutes { get; set; }

    public string Language { get; set; } = "fr";
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
