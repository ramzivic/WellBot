using WellBot.Shared.Enums;

namespace WellBot.Admin.Entities;

public class AnalyticsEventEntity
{
    public int Id { get; set; }
    public string MachineId { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public AnalyticsAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public long SessionDurationSeconds { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
