namespace WellBot.Shared.DTOs;

/// <summary>
/// Configuration complète récupérée par le client desktop au démarrage.
/// </summary>
public class ClientConfigDto
{
    /// <summary>Configurations des notifications par type et par langue.</summary>
    public List<NotificationConfigDto> Notifications { get; set; } = new();

    /// <summary>Conseils santé disponibles.</summary>
    public List<HealthTipDto> HealthTips { get; set; } = new();

    /// <summary>Paramètres d'animation.</summary>
    public AnimationSettingsDto AnimationSettings { get; set; } = new();
}
