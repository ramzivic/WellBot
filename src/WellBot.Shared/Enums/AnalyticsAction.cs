namespace WellBot.Shared.Enums;

/// <summary>
/// Actions trackées pour les analytics.
/// </summary>
public enum AnalyticsAction
{
    /// <summary>La notification a été affichée.</summary>
    Displayed,

    /// <summary>L'utilisateur a cliqué "J'ai compris".</summary>
    Acknowledged,

    /// <summary>L'utilisateur a fermé manuellement la notification.</summary>
    Dismissed,

    /// <summary>La notification s'est auto-fermée après timeout.</summary>
    Timeout,

    /// <summary>L'utilisateur a changé d'avatar.</summary>
    AvatarChanged,

    /// <summary>Le mode DND a été activé.</summary>
    DndActivated,

    /// <summary>Le mode DND a été désactivé.</summary>
    DndDeactivated
}
