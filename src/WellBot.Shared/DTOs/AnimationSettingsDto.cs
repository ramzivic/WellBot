namespace WellBot.Shared.DTOs;

public class AnimationSettingsDto
{
    public double StretchingScale { get; set; } = 3.0;
    public double ActiveBreakScale { get; set; } = 1.4;
    public double DefaultStartDelay { get; set; } = 2.0;
    public bool OverrideServerSettings { get; set; } = false;
}
