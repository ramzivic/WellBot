using System;
using System.Collections.Generic;
using System.Windows.Threading;
using WellBot.Shared.Enums;
using WellBot.Shared.DTOs;
using System.Linq;

namespace WellBot.Desktop.Services;

public interface INotificationScheduler
{
    void Start();
    void Stop();
    void UpdateConfig(List<NotificationConfigDto> configs);
}

public class NotificationScheduler : INotificationScheduler
{
    private readonly List<NotificationTimer> _timers = new();
    private readonly Action<NotificationConfigDto> _onTrigger;

    public NotificationScheduler(Action<NotificationConfigDto> onTrigger)
    {
        _onTrigger = onTrigger;
    }

    public void Start()
    {
        foreach (var timer in _timers)
        {
            timer.Start();
        }
    }

    public void Stop()
    {
        foreach (var timer in _timers)
        {
            timer.Stop();
        }
    }

    public void UpdateConfig(List<NotificationConfigDto> configs)
    {
        Stop();
        _timers.Clear();

        foreach (var config in configs.Where(c => c.IsEnabled || c.Type == NotificationType.HealthTip))
        {
            var timer = new NotificationTimer(config, _onTrigger);
            _timers.Add(timer);
        }
        
        Start();
    }

    private class NotificationTimer
    {
        private readonly DispatcherTimer _timer;
        private readonly NotificationConfigDto _config;
        private readonly Action<NotificationConfigDto> _onTrigger;

        public NotificationTimer(NotificationConfigDto config, Action<NotificationConfigDto> onTrigger)
        {
            _config = config;
            _onTrigger = onTrigger;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(config.IntervalMinutes)
            };
            _timer.Tick += (s, e) => _onTrigger(_config);
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();
    }
}
