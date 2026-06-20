using WellBot.Shared.Enums;

namespace WellBot.Shared.DTOs;

/// <summary>
/// Configuration d'un type de notification (intervalle, messages, etc.).
/// </summary>
public class NotificationConfigDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Nom de l'animation Lottie à jouer (drink, look_away, stretch, walk, breathe).</summary>
    public string AnimationName { get; set; } = string.Empty;

    /// <summary>Intervalle entre les notifications en minutes.</summary>
    public int IntervalMinutes { get; set; }

    /// <summary>Code langue (fr, en, ar).</summary>
    public string Language { get; set; } = "fr";

    public bool IsEnabled { get; set; } = true;
}
