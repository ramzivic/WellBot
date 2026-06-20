namespace WellBot.Shared.DTOs;

/// <summary>
/// Conseil santé des médecins du travail.
/// </summary>
public class HealthTipDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Code langue (fr, en, ar).</summary>
    public string Language { get; set; } = "fr";

    public bool IsActive { get; set; } = true;
}
