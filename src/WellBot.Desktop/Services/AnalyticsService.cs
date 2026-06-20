using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WellBot.Shared.DTOs;
using WellBot.Shared.Enums;

namespace WellBot.Desktop.Services;

public interface IAnalyticsService
{
    void TrackEvent(NotificationType type, AnalyticsAction action);
    void Start();
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IWellBotApiClient _apiClient;
    private readonly List<AnalyticsEventDto> _queue = new();
    private readonly DispatcherTimer _timer;
    private readonly string _machineId;

    public AnalyticsService(IWellBotApiClient apiClient)
    {
        _apiClient = apiClient;
        _machineId = GetMachineId();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _timer.Tick += async (s, e) => await FlushAsync();
    }

    public async void Start()
    {
        _timer.Start();
        // Track a heartbeat event to register the machine immediately
        TrackEvent(NotificationType.HealthTip, AnalyticsAction.Displayed); // Minimal event to show presence
        await FlushAsync();
    }

    public void TrackEvent(NotificationType type, AnalyticsAction action)
    {
        var ev = new AnalyticsEventDto
        {
            MachineId = _machineId,
            NotificationType = type,
            Action = action,
            Timestamp = DateTime.UtcNow,
            SessionDurationSeconds = (long)(DateTime.UtcNow - GetBootTime()).TotalSeconds
        };

        lock (_queue)
        {
            _queue.Add(ev);
        }
    }

    private async Task FlushAsync()
    {
        List<AnalyticsEventDto> toSend;
        lock (_queue)
        {
            if (!_queue.Any()) return;
            toSend = _queue.ToList();
            _queue.Clear();
        }

        await _apiClient.PostAnalyticsEventsAsync(toSend);
    }

    private string GetMachineId()
    {
        try
        {
            string name = Environment.MachineName;
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(name));
            return Convert.ToHexString(hash).Substring(0, 16);
        }
        catch
        {
            return "unknown";
        }
    }

    private DateTime GetBootTime()
    {
        return DateTime.UtcNow.AddMilliseconds(-Environment.TickCount64);
    }
}
