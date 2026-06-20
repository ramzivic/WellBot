using WellBot.Shared.Enums;

namespace WellBot.Shared.DTOs;

/// <summary>
/// Événement d'interaction utilisateur envoyé au serveur pour analytics.
/// </summary>
public class AnalyticsEventDto
{
    /// <summary>Identifiant anonyme de la machine (hash).</summary>
    public string MachineId { get; set; } = string.Empty;

    public NotificationType NotificationType { get; set; }
    public AnalyticsAction Action { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Durée de la session machine en secondes depuis le dernier boot.</summary>
    public long SessionDurationSeconds { get; set; }
}
