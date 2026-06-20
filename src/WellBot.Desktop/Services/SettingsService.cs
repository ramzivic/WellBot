using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WellBot.Shared.DTOs;
using WellBot.Shared.Enums;

namespace WellBot.Desktop.Services;

public interface ISettingsService
{
    Task SaveConfigCacheAsync(ClientConfigDto config);
    Task<ClientConfigDto?> LoadConfigCacheAsync();
    string GetLanguage();
    void SetLanguage(string language);
    AvatarType GetAvatar();
    void SetAvatar(AvatarType avatar);
    string GetCustomAvatarPath();
    void SetCustomAvatarPath(string path);
    string GetUserName();
    void SetUserName(string name);
    bool IsFirstLaunch();
    void SetFirstLaunch(bool value);
    AnimationSettingsDto GetAnimationSettings();
    void SetAnimationSettings(AnimationSettingsDto settings);
    bool IsTtsEnabled();
    void SetTtsEnabled(bool value);
    void ApplyLanguage();
    event Action? LanguageChanged;

    /// <summary>Local notification schedules (used when disconnected from server).</summary>
    List<LocalNotificationSchedule> GetLocalSchedules();
    void SetLocalSchedules(List<LocalNotificationSchedule> schedules);

    string GetServerUrl();
    string GetServerUsername();
    string GetServerPassword();

    TimeSpan GetWorkStartTime();
    void SetWorkStartTime(TimeSpan time);
    TimeSpan GetWorkEndTime();
    void SetWorkEndTime(TimeSpan time);
    List<DayOfWeek> GetWorkDays();
    void SetWorkDays(List<DayOfWeek> days);
    bool IsWithinWorkingHours();
}

/// <summary>
/// Represents a user-defined local notification schedule (offline mode).
/// </summary>
public class LocalNotificationSchedule
{
    public NotificationType Type { get; set; }
    public int IntervalMinutes { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class SettingsService : ISettingsService
{
    private readonly string _cachePath;
    private readonly string _settingsPath;
    private string _language = "fr";
    private AvatarType _avatar = AvatarType.Bot1;
    private string _customAvatarPath = string.Empty;
    private string _userName = GetDefaultDisplayName();
    private bool _isFirstLaunch = true;
    private AnimationSettingsDto _animationSettings = new();
    private bool _isTtsEnabled = false;
    private List<LocalNotificationSchedule> _localSchedules = GetDefaultSchedules();
    private string _serverUrl = "http://localhost:5191";
    private string _serverUsername = "wb_svc_d3sk";
    private string _serverPassword = "Sv@WbD3sk!7mNx#Qr2026";
    private TimeSpan _workStartTime = new TimeSpan(8, 30, 0); // 08:30
    private TimeSpan _workEndTime = new TimeSpan(17, 0, 0); // 17:00
    private List<DayOfWeek> _workDays = new() { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }; // Sun-Thu

    public event Action? LanguageChanged;

    private static string GetDefaultDisplayName()
    {
        try
        {
            var displayName = System.DirectoryServices.AccountManagement.UserPrincipal.Current?.DisplayName;
            if (!string.IsNullOrEmpty(displayName)) return displayName;
        }
        catch { }
        return Environment.UserName;
    }

    private static List<LocalNotificationSchedule> GetDefaultSchedules()
    {
        return new List<LocalNotificationSchedule>
        {
            new() { Type = NotificationType.Hydration, IntervalMinutes = 120, IsEnabled = true },
            new() { Type = NotificationType.VisualBreak, IntervalMinutes = 60, IsEnabled = true },
            new() { Type = NotificationType.Stretching, IntervalMinutes = 120, IsEnabled = true },
            new() { Type = NotificationType.ActiveBreak, IntervalMinutes = 180, IsEnabled = true },
            new() { Type = NotificationType.Breathing, IntervalMinutes = 120, IsEnabled = true },
            new() { Type = NotificationType.HealthTip, IntervalMinutes = 60, IsEnabled = true }
        };
    }

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var wellBotDir = Path.Combine(appData, "WellBot");
        Directory.CreateDirectory(wellBotDir);
        _cachePath = Path.Combine(wellBotDir, "config_cache.json");
        _settingsPath = Path.Combine(wellBotDir, "settings.json");
        LoadSettings();
        ApplyLanguage();
    }

    public void ApplyLanguage()
    {
        var cultureCode = _language switch
        {
            "en" => "en-US",
            "ar" => "ar-DZ",
            _ => "fr-FR"
        };
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        WellBot.Desktop.Resources.AppResources.Culture = culture;
        LanguageChanged?.Invoke();
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Language", out var lang)) _language = lang.GetString() ?? "fr";
                if (doc.RootElement.TryGetProperty("Avatar", out var av)) _avatar = (AvatarType)av.GetInt32();
                if (doc.RootElement.TryGetProperty("CustomAvatarPath", out var cap)) _customAvatarPath = cap.GetString() ?? string.Empty;
                if (doc.RootElement.TryGetProperty("UserName", out var un)) _userName = un.GetString() ?? Environment.UserName;
                if (doc.RootElement.TryGetProperty("IsFirstLaunch", out var fl)) _isFirstLaunch = fl.GetBoolean();
                if (doc.RootElement.TryGetProperty("AnimationSettings", out var animElement)) 
                {
                    _animationSettings = JsonSerializer.Deserialize<AnimationSettingsDto>(animElement.GetRawText()) ?? new();
                }
                if (doc.RootElement.TryGetProperty("IsTtsEnabled", out var tts)) _isTtsEnabled = tts.GetBoolean();
                if (doc.RootElement.TryGetProperty("LocalSchedules", out var schedElement))
                {
                    _localSchedules = JsonSerializer.Deserialize<List<LocalNotificationSchedule>>(schedElement.GetRawText()) ?? GetDefaultSchedules();
                }
                if (doc.RootElement.TryGetProperty("ServerUrl", out var surl)) _serverUrl = surl.GetString() ?? "http://localhost:5191";
                if (doc.RootElement.TryGetProperty("ServerUsername", out var suname))
                {
                    var rawUsername = suname.GetString() ?? "";
                    var decrypted = CredentialProtector.Unprotect(rawUsername);
                    if (!string.IsNullOrEmpty(decrypted)) _serverUsername = decrypted;
                }
                if (doc.RootElement.TryGetProperty("ServerPassword", out var spwd))
                {
                    var rawPassword = spwd.GetString() ?? "";
                    var decrypted = CredentialProtector.Unprotect(rawPassword);
                    if (!string.IsNullOrEmpty(decrypted)) _serverPassword = decrypted;
                }
                if (doc.RootElement.TryGetProperty("WorkStartTime", out var wst)) _workStartTime = TimeSpan.Parse(wst.GetString() ?? "08:00:00");
                if (doc.RootElement.TryGetProperty("WorkEndTime", out var wet)) _workEndTime = TimeSpan.Parse(wet.GetString() ?? "18:00:00");
                if (doc.RootElement.TryGetProperty("WorkDays", out var wds))
                {
                    _workDays = JsonSerializer.Deserialize<List<DayOfWeek>>(wds.GetRawText()) ?? _workDays;
                }
            }
            catch { }
        }
    }

    private void SaveSettings()
    {
        var settings = new 
        { 
            Language = _language, 
            Avatar = (int)_avatar, 
            CustomAvatarPath = _customAvatarPath,
            UserName = _userName,
            IsFirstLaunch = _isFirstLaunch,
            AnimationSettings = _animationSettings,
            IsTtsEnabled = _isTtsEnabled,
            LocalSchedules = _localSchedules,
            ServerUrl = _serverUrl,
            ServerUsername = CredentialProtector.Protect(_serverUsername),
            ServerPassword = CredentialProtector.Protect(_serverPassword),
            WorkStartTime = _workStartTime.ToString(@"hh\:mm\:ss"),
            WorkEndTime = _workEndTime.ToString(@"hh\:mm\:ss"),
            WorkDays = _workDays
        };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));
    }

    public async Task SaveConfigCacheAsync(ClientConfigDto config)
    {
        var json = JsonSerializer.Serialize(config);
        await File.WriteAllTextAsync(_cachePath, json);
    }

    public async Task<ClientConfigDto?> LoadConfigCacheAsync()
    {
        if (!File.Exists(_cachePath)) return null;
        var json = await File.ReadAllTextAsync(_cachePath);
        return JsonSerializer.Deserialize<ClientConfigDto>(json);
    }

    public string GetLanguage() => _language;
    public void SetLanguage(string language) { _language = language; SaveSettings(); ApplyLanguage(); }

    public AvatarType GetAvatar() => _avatar;
    public void SetAvatar(AvatarType avatar) { _avatar = avatar; SaveSettings(); }

    public string GetCustomAvatarPath() => _customAvatarPath;
    public void SetCustomAvatarPath(string path) { _customAvatarPath = path; SaveSettings(); }

    public string GetUserName() => _userName;
    public void SetUserName(string name) { _userName = name; SaveSettings(); }

    public bool IsFirstLaunch() => _isFirstLaunch;
    public void SetFirstLaunch(bool value) { _isFirstLaunch = value; SaveSettings(); }

    public AnimationSettingsDto GetAnimationSettings() => _animationSettings;
    public void SetAnimationSettings(AnimationSettingsDto settings) { _animationSettings = settings; SaveSettings(); }

    public bool IsTtsEnabled() => _isTtsEnabled;
    public void SetTtsEnabled(bool value) { _isTtsEnabled = value; SaveSettings(); }

    public List<LocalNotificationSchedule> GetLocalSchedules() => _localSchedules;
    public void SetLocalSchedules(List<LocalNotificationSchedule> schedules) { _localSchedules = schedules; SaveSettings(); }

    public string GetServerUrl() => _serverUrl;
    public string GetServerUsername() => _serverUsername;
    public string GetServerPassword() => _serverPassword;

    public TimeSpan GetWorkStartTime() => _workStartTime;
    public void SetWorkStartTime(TimeSpan time) { _workStartTime = time; SaveSettings(); }

    public TimeSpan GetWorkEndTime() => _workEndTime;
    public void SetWorkEndTime(TimeSpan time) { _workEndTime = time; SaveSettings(); }

    public List<DayOfWeek> GetWorkDays() => _workDays;
    public void SetWorkDays(List<DayOfWeek> days) { _workDays = days; SaveSettings(); }

    public bool IsWithinWorkingHours()
    {
        var now = DateTime.Now;
        if (!_workDays.Contains(now.DayOfWeek)) return false;
        var time = now.TimeOfDay;
        if (_workStartTime < _workEndTime)
        {
            return time >= _workStartTime && time <= _workEndTime;
        }
        else // Handles overnight shifts e.g. 22:00 to 06:00
        {
            return time >= _workStartTime || time <= _workEndTime;
        }
    }
}
