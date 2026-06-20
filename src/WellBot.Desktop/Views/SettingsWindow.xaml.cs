using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WellBot.Desktop.Services;
using WellBot.Shared.DTOs;
using WellBot.Shared.Enums;
using System.Collections.Generic;
using System;

namespace WellBot.Desktop.Views;

public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;
    private readonly IWellBotApiClient _apiClient;
    private readonly INotificationScheduler _scheduler;
    private bool _isSaved = false;
    private List<LocalNotificationSchedule> _localSchedules;

    public SettingsWindow(ISettingsService settingsService, IWellBotApiClient apiClient, INotificationScheduler scheduler)
    {
        _settingsService = settingsService;
        _apiClient = apiClient;
        _scheduler = scheduler;
        _localSchedules = _settingsService.GetLocalSchedules();
        InitializeComponent();
        
        _apiClient.ConnectionStatusChanged += ApiClient_ConnectionStatusChanged;
        
        LoadCurrentSettings();
        UpdateConnectionStatus(_apiClient.IsConnected, _apiClient.LastError);

        this.Loaded += async (s, e) => 
        {
            TxtStatusMessage.Text = "Vérification de la connexion...";
            TxtStatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDD6F4"));
            var config = await _apiClient.GetConfigAsync(_settingsService.GetLanguage());
            if (config != null && config.Notifications != null)
            {
                await _settingsService.SaveConfigCacheAsync(config);
                _scheduler.UpdateConfig(config.Notifications);
                
                Dispatcher.Invoke(() => 
                {
                    UpdateSchedulesFromServer(config.Notifications);
                    TxtStatusMessage.Text = $"Connecté au serveur Admin (Synchro OK - {config.Notifications.Count} notifs)";
                });
            }
        };
    }

    private void ApiClient_ConnectionStatusChanged(bool isConnected, string? error)
    {
        Dispatcher.Invoke(() => UpdateConnectionStatus(isConnected, error));
    }

    private void UpdateConnectionStatus(bool isConnected, string? error)
    {
        if (isConnected)
        {
            StatusBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E4035")); // Greenish
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1")); // Green
            TxtStatusMessage.Text = WellBot.Desktop.Resources.AppResources.StatusConnected;
            TxtStatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1"));
            TxtStatusDetail.Visibility = Visibility.Collapsed;
            
            // Grey out schedule section
            SchedulesSection.Opacity = 0.5;
            SchedulesSection.IsEnabled = false;
            TxtScheduleServerInfo.Visibility = Visibility.Visible;
        }
        else
        {
            StatusBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A2C35")); // Reddish
            StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8")); // Red
            TxtStatusMessage.Text = WellBot.Desktop.Resources.AppResources.StatusOffline;
            TxtStatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"));
            TxtStatusDetail.Visibility = !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed;
            
            // Enable schedule section
            SchedulesSection.Opacity = 1.0;
            SchedulesSection.IsEnabled = true;
            TxtScheduleServerInfo.Visibility = Visibility.Collapsed;
        }
    }

    private void StatusBanner_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_apiClient.IsConnected && !string.IsNullOrEmpty(_apiClient.LastError))
        {
            MessageBox.Show($"Erreur de connexion détaillée:\n\n{_apiClient.LastError}", "Erreur de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StatusDetail_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Handled by StatusBanner_Click as event bubbles up
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _apiClient.ConnectionStatusChanged -= ApiClient_ConnectionStatusChanged;
        base.OnClosed(e);
        if (!_isSaved)
        {
            _settingsService.ApplyLanguage();
        }
    }

    private void LoadCurrentSettings()
    {
        UpdateTexts();

        // General
        TxtUserName.Text = _settingsService.GetUserName();

        var currentLang = _settingsService.GetLanguage();
        foreach (ComboBoxItem item in CmbLanguage.Items)
        {
            if ((string)item.Tag == currentLang)
            {
                CmbLanguage.SelectedItem = item;
                break;
            }
        }
        if (CmbLanguage.SelectedItem == null && CmbLanguage.Items.Count > 0)
            CmbLanguage.SelectedIndex = 0;

        // Server connection
        TxtServerUrl.Text = _settingsService.GetServerUrl();

        // Work Hours
        TxtWorkStartTime.Text = _settingsService.GetWorkStartTime().ToString(@"hh\:mm");
        TxtWorkEndTime.Text = _settingsService.GetWorkEndTime().ToString(@"hh\:mm");
        var workDays = _settingsService.GetWorkDays();
        ChbSun.IsChecked = workDays.Contains(DayOfWeek.Sunday);
        ChbMon.IsChecked = workDays.Contains(DayOfWeek.Monday);
        ChbTue.IsChecked = workDays.Contains(DayOfWeek.Tuesday);
        ChbWed.IsChecked = workDays.Contains(DayOfWeek.Wednesday);
        ChbThu.IsChecked = workDays.Contains(DayOfWeek.Thursday);
        ChbFri.IsChecked = workDays.Contains(DayOfWeek.Friday);
        ChbSat.IsChecked = workDays.Contains(DayOfWeek.Saturday);

        // Animation settings
        var animSettings = _settingsService.GetAnimationSettings();
        TxtStretchScale.Text = animSettings.StretchingScale.ToString(CultureInfo.InvariantCulture);
        TxtActiveBreakScale.Text = animSettings.ActiveBreakScale.ToString(CultureInfo.InvariantCulture);
        TxtStartDelay.Text = animSettings.DefaultStartDelay.ToString(CultureInfo.InvariantCulture);
        ChkOverrideServer.IsChecked = animSettings.OverrideServerSettings;

        // TTS
        ChkTtsEnabled.IsChecked = _settingsService.IsTtsEnabled();

        // Schedules
        LoadSchedule(NotificationType.Hydration, ChkSchedHydration, TxtSchedHydrationMin);
        LoadSchedule(NotificationType.VisualBreak, ChkSchedVisual, TxtSchedVisualMin);
        LoadSchedule(NotificationType.Stretching, ChkSchedStretch, TxtSchedStretchMin);
        LoadSchedule(NotificationType.ActiveBreak, ChkSchedActive, TxtSchedActiveMin);
        LoadSchedule(NotificationType.Breathing, ChkSchedBreathing, TxtSchedBreathingMin);
        LoadSchedule(NotificationType.HealthTip, ChkSchedHealth, TxtSchedHealthMin);
    }

    private void LoadSchedule(NotificationType type, CheckBox chk, TextBox txt)
    {
        var schedule = _localSchedules.FirstOrDefault(s => s.Type == type);
        if (schedule != null)
        {
            chk.IsChecked = schedule.IsEnabled;
            txt.Text = schedule.IntervalMinutes.ToString();
        }
    }

    private void UpdateSchedulesFromServer(List<NotificationConfigDto> serverConfigs)
    {
        void Update(NotificationType type, CheckBox chk, TextBox txt)
        {
            var conf = serverConfigs.FirstOrDefault(c => c.Type == type);
            if (conf != null)
            {
                chk.IsChecked = conf.IsEnabled;
                txt.Text = conf.IntervalMinutes.ToString();
            }
        }

        Update(NotificationType.Hydration, ChkSchedHydration, TxtSchedHydrationMin);
        Update(NotificationType.VisualBreak, ChkSchedVisual, TxtSchedVisualMin);
        Update(NotificationType.Stretching, ChkSchedStretch, TxtSchedStretchMin);
        Update(NotificationType.ActiveBreak, ChkSchedActive, TxtSchedActiveMin);
        Update(NotificationType.Breathing, ChkSchedBreathing, TxtSchedBreathingMin);
        Update(NotificationType.HealthTip, ChkSchedHealth, TxtSchedHealthMin);
    }

    private void SaveSchedule(NotificationType type, CheckBox chk, TextBox txt)
    {
        var schedule = _localSchedules.FirstOrDefault(s => s.Type == type);
        if (schedule == null)
        {
            schedule = new LocalNotificationSchedule { Type = type };
            _localSchedules.Add(schedule);
        }
        
        schedule.IsEnabled = chk.IsChecked == true;
        if (int.TryParse(txt.Text, out int minutes) && minutes > 0)
        {
            schedule.IntervalMinutes = minutes;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _isSaved = true;
        // Save general
        _settingsService.SetUserName(TxtUserName.Text.Trim());

        if (CmbLanguage.SelectedItem is ComboBoxItem selectedLang)
        {
            _settingsService.SetLanguage((string)selectedLang.Tag);
        }

        // Save Work Hours
        string[] timeFormats = { @"h\:mm", @"hh\:mm" };
        if (!TimeSpan.TryParseExact(TxtWorkStartTime.Text.Trim(), timeFormats, CultureInfo.InvariantCulture, out TimeSpan start) ||
            !TimeSpan.TryParseExact(TxtWorkEndTime.Text.Trim(), timeFormats, CultureInfo.InvariantCulture, out TimeSpan end))
        {
            MessageBox.Show(WellBot.Desktop.Resources.AppResources.InvalidTimeFormat, "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settingsService.SetWorkStartTime(start);
        _settingsService.SetWorkEndTime(end);

        var days = new List<DayOfWeek>();
        if (ChbSun.IsChecked == true) days.Add(DayOfWeek.Sunday);
        if (ChbMon.IsChecked == true) days.Add(DayOfWeek.Monday);
        if (ChbTue.IsChecked == true) days.Add(DayOfWeek.Tuesday);
        if (ChbWed.IsChecked == true) days.Add(DayOfWeek.Wednesday);
        if (ChbThu.IsChecked == true) days.Add(DayOfWeek.Thursday);
        if (ChbFri.IsChecked == true) days.Add(DayOfWeek.Friday);
        if (ChbSat.IsChecked == true) days.Add(DayOfWeek.Saturday);
        _settingsService.SetWorkDays(days);

        // Save animation settings
        var animSettings = _settingsService.GetAnimationSettings();

        if (double.TryParse(TxtStretchScale.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var stretchScale))
            animSettings.StretchingScale = stretchScale;

        if (double.TryParse(TxtActiveBreakScale.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var activeScale))
            animSettings.ActiveBreakScale = activeScale;

        if (double.TryParse(TxtStartDelay.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var delay))
            animSettings.DefaultStartDelay = delay;

        animSettings.OverrideServerSettings = ChkOverrideServer.IsChecked == true;

        _settingsService.SetAnimationSettings(animSettings);

        // Save TTS
        _settingsService.SetTtsEnabled(ChkTtsEnabled.IsChecked == true);

        // Save Schedules
        SaveSchedule(NotificationType.Hydration, ChkSchedHydration, TxtSchedHydrationMin);
        SaveSchedule(NotificationType.VisualBreak, ChkSchedVisual, TxtSchedVisualMin);
        SaveSchedule(NotificationType.Stretching, ChkSchedStretch, TxtSchedStretchMin);
        SaveSchedule(NotificationType.ActiveBreak, ChkSchedActive, TxtSchedActiveMin);
        SaveSchedule(NotificationType.Breathing, ChkSchedBreathing, TxtSchedBreathingMin);
        SaveSchedule(NotificationType.HealthTip, ChkSchedHealth, TxtSchedHealthMin);
        
        _settingsService.SetLocalSchedules(_localSchedules);

        if (!_apiClient.IsConnected)
        {
            var offlineDtos = new List<NotificationConfigDto>();
            foreach (var s in _localSchedules)
            {
                offlineDtos.Add(new NotificationConfigDto
                {
                    Type = s.Type,
                    IntervalMinutes = s.IntervalMinutes,
                    IsEnabled = s.IsEnabled,
                    Language = _settingsService.GetLanguage()
                });
            }
            _scheduler.UpdateConfig(offlineDtos);
        }

        this.Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbLanguage.SelectedItem is ComboBoxItem selectedLang)
        {
            var lang = (string)selectedLang.Tag;
            try
            {
                var culture = new CultureInfo(lang);
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                WellBot.Desktop.Resources.AppResources.Culture = culture;
                UpdateTexts();
            }
            catch {}
        }
    }

    private void UpdateTexts()
    {
        if (TxtSettingsTitle != null) TxtSettingsTitle.Text = WellBot.Desktop.Resources.AppResources.SettingsTitle;
        if (TxtSectionGeneral != null) TxtSectionGeneral.Text = WellBot.Desktop.Resources.AppResources.SettingsGeneral;
        if (TxtLblUserName != null) TxtLblUserName.Text = WellBot.Desktop.Resources.AppResources.SettingsUserName;
        if (TxtLblLanguage != null) TxtLblLanguage.Text = WellBot.Desktop.Resources.AppResources.SettingsLanguage;
        if (TxtSectionAnimations != null) TxtSectionAnimations.Text = WellBot.Desktop.Resources.AppResources.SettingsAnimations;
        if (TxtLblStretchScale != null) TxtLblStretchScale.Text = WellBot.Desktop.Resources.AppResources.SettingsStretchScale;
        if (TxtLblActiveBreakScale != null) TxtLblActiveBreakScale.Text = WellBot.Desktop.Resources.AppResources.SettingsActiveBreakScale;
        if (TxtLblStartDelay != null) TxtLblStartDelay.Text = WellBot.Desktop.Resources.AppResources.SettingsStartDelay;
        if (TxtLblOverrideServer != null) TxtLblOverrideServer.Text = WellBot.Desktop.Resources.AppResources.SettingsOverrideServer;
        
        if (TxtSectionServer != null) TxtSectionServer.Text = WellBot.Desktop.Resources.AppResources.SettingsServerConn;
        if (TxtLblServerUrl != null) TxtLblServerUrl.Text = WellBot.Desktop.Resources.AppResources.SettingsServerUrl;
        
        if (TxtSectionSchedules != null) TxtSectionSchedules.Text = WellBot.Desktop.Resources.AppResources.SettingsSchedules;
        if (TxtScheduleServerInfo != null) TxtScheduleServerInfo.Text = WellBot.Desktop.Resources.AppResources.SettingsScheduleServerInfo;
        
        if (TxtSchedHydration != null) TxtSchedHydration.Text = WellBot.Desktop.Resources.AppResources.SchedHydration;
        if (TxtSchedVisual != null) TxtSchedVisual.Text = WellBot.Desktop.Resources.AppResources.SchedVisual;
        if (TxtSchedStretch != null) TxtSchedStretch.Text = WellBot.Desktop.Resources.AppResources.SchedStretch;
        if (TxtSchedActive != null) TxtSchedActive.Text = WellBot.Desktop.Resources.AppResources.SchedActive;
        if (TxtSchedBreathing != null) TxtSchedBreathing.Text = WellBot.Desktop.Resources.AppResources.SchedBreathing;
        if (TxtSchedHealth != null) TxtSchedHealth.Text = WellBot.Desktop.Resources.AppResources.SchedHealth;

        if (TxtSectionWorkHours != null) TxtSectionWorkHours.Text = WellBot.Desktop.Resources.AppResources.SettingsWorkHours;
        if (TxtLblWorkStartTime != null) TxtLblWorkStartTime.Text = WellBot.Desktop.Resources.AppResources.SettingsWorkStartTime;
        if (TxtLblWorkEndTime != null) TxtLblWorkEndTime.Text = WellBot.Desktop.Resources.AppResources.SettingsWorkEndTime;
        if (TxtLblWorkDays != null) TxtLblWorkDays.Text = WellBot.Desktop.Resources.AppResources.SettingsWorkDays;
        
        if (ChbSun != null) ChbSun.Content = WellBot.Desktop.Resources.AppResources.DaySun;
        if (ChbMon != null) ChbMon.Content = WellBot.Desktop.Resources.AppResources.DayMon;
        if (ChbTue != null) ChbTue.Content = WellBot.Desktop.Resources.AppResources.DayTue;
        if (ChbWed != null) ChbWed.Content = WellBot.Desktop.Resources.AppResources.DayWed;
        if (ChbThu != null) ChbThu.Content = WellBot.Desktop.Resources.AppResources.DayThu;
        if (ChbFri != null) ChbFri.Content = WellBot.Desktop.Resources.AppResources.DayFri;
        if (ChbSat != null) ChbSat.Content = WellBot.Desktop.Resources.AppResources.DaySat;

        if (TxtSchedHydrationUnit != null) TxtSchedHydrationUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        if (TxtSchedVisualUnit != null) TxtSchedVisualUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        if (TxtSchedStretchUnit != null) TxtSchedStretchUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        if (TxtSchedActiveUnit != null) TxtSchedActiveUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        if (TxtSchedBreathingUnit != null) TxtSchedBreathingUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        if (TxtSchedHealthUnit != null) TxtSchedHealthUnit.Text = WellBot.Desktop.Resources.AppResources.SchedUnitMin;
        
        if (TxtSectionAccessibility != null) TxtSectionAccessibility.Text = WellBot.Desktop.Resources.AppResources.SettingsAccessibility;
        if (TxtLblTtsEnabled != null) TxtLblTtsEnabled.Text = WellBot.Desktop.Resources.AppResources.SettingsTtsEnabled;
        
        if (TxtStatusDetail != null) TxtStatusDetail.Text = WellBot.Desktop.Resources.AppResources.StatusDetails;
        UpdateConnectionStatus(_apiClient.IsConnected, _apiClient.LastError);

        if (BtnCancel != null) BtnCancel.Content = WellBot.Desktop.Resources.AppResources.SettingsCancel;
        if (BtnSave != null) BtnSave.Content = WellBot.Desktop.Resources.AppResources.SettingsSave;
        this.Title = WellBot.Desktop.Resources.AppResources.SettingsTitle;
    }
}
