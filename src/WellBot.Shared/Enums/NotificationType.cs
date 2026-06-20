namespace WellBot.Shared.Enums;

/// <summary>
/// Types de notifications bien-être supportés par WellBot.
/// </summary>
public enum NotificationType
{
    /// <summary>Rappel d'hydratation (boire de l'eau).</summary>
    Hydration,

    /// <summary>Pause visuelle (regarder au loin).</summary>
    VisualBreak,

    /// <summary>Exercices d'étirement.</summary>
    Stretching,

    /// <summary>Pause active (petite promenade).</summary>
    ActiveBreak,

    /// <summary>Exercice de respiration profonde.</summary>
    Breathing,

    /// <summary>Conseil santé des médecins du travail.</summary>
    HealthTip
}
