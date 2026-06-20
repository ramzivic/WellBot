using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;
using WellBot.Shared.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace WellBot.Desktop.Services;

public interface IDndDetectorService
{
    bool IsDndActive { get; set; }
    bool IsSessionLocked { get; }
    event Action<bool>? OnDndStateChanged;
    event Action? OnSessionUnlocked;
    void Start();
    void Stop();
    
    void AddMissedNotification(NotificationConfigDto config);
    IEnumerable<NotificationConfigDto> GetMissedNotifications();
    void ClearMissedNotifications();
}

public class DndDetectorService : IDndDetectorService
{
    private bool _isDndActive;
    private bool _manualDnd;
    private bool _isSessionLocked;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<WellBot.Shared.Enums.NotificationType, NotificationConfigDto> _missedNotifications = new();

    public bool IsDndActive
    {
        get => _isDndActive || _manualDnd;
        set
        {
            _manualDnd = value;
            NotifyChange();
        }
    }

    public bool IsSessionLocked => _isSessionLocked;

    public event Action<bool>? OnDndStateChanged;
    public event Action? OnSessionUnlocked;

    public DndDetectorService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (s, e) => CheckSystemDnd();
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
    }

    private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock)
        {
            _isSessionLocked = true;
            NotifyChange();
        }
        else if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _isSessionLocked = false;
            NotifyChange();
            OnSessionUnlocked?.Invoke();
        }
    }

    public void AddMissedNotification(NotificationConfigDto config)
    {
        if (_isSessionLocked)
        {
            _missedNotifications[config.Type] = config;
        }
    }

    public IEnumerable<NotificationConfigDto> GetMissedNotifications()
    {
        return _missedNotifications.Values.ToList();
    }

    public void ClearMissedNotifications()
    {
        _missedNotifications.Clear();
    }

    public void Start() => _timer.Start();
    public void Stop()
    {
        _timer.Stop();
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }

    private void CheckSystemDnd()
    {
        bool currentDnd = IsFullScreenAppRunning();
        
        if (currentDnd != _isDndActive)
        {
            _isDndActive = currentDnd;
            NotifyChange();
        }
    }

    private void NotifyChange()
    {
        OnDndStateChanged?.Invoke(IsDndActive);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    private bool IsFullScreenAppRunning()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;
        if (foregroundWindow == GetDesktopWindow() || foregroundWindow == GetShellWindow()) return false;

        RECT rect;
        if (GetWindowRect(foregroundWindow, out rect))
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            
            return width >= System.Windows.SystemParameters.PrimaryScreenWidth && 
                   height >= System.Windows.SystemParameters.PrimaryScreenHeight;
        }

        return false;
    }
}
