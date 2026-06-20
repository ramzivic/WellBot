using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WellBot.Desktop.Services;
using System.Windows;
using WellBot.Desktop.Views;
using WellBot.Desktop.Resources;

namespace WellBot.Desktop.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IDndDetectorService _dndService;
    private readonly ISettingsService _settingsService;
    private readonly IWellBotApiClient _apiClient;
    private readonly INotificationScheduler _scheduler;

    public MainViewModel(IDndDetectorService dndService, ISettingsService settingsService, IWellBotApiClient apiClient, INotificationScheduler scheduler)
    {
        _dndService = dndService;
        _settingsService = settingsService;
        _apiClient = apiClient;
        _scheduler = scheduler;
        Title = "WellBot";
        
        _settingsService.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(MenuOpenDashboardText));
            OnPropertyChanged(nameof(MenuSettingsText));
            OnPropertyChanged(nameof(MenuChangeAvatarText));
            OnPropertyChanged(nameof(MenuDndText));
            OnPropertyChanged(nameof(MenuQuitText));
            OnPropertyChanged(nameof(TrayTooltipText));
        };
    }

    public string MenuOpenDashboardText => AppResources.MenuOpenDashboard;
    public string MenuSettingsText => AppResources.MenuSettings;
    public string MenuChangeAvatarText => AppResources.MenuChangeAvatar;
    public string MenuDndText => AppResources.MenuDnd;
    public string MenuQuitText => AppResources.MenuQuit;
    public string TrayTooltipText => AppResources.TrayTooltip;

    public bool IsDndActive
    {
        get => _dndService.IsDndActive;
        set => _dndService.IsDndActive = value;
    }

    [RelayCommand]
    private void Open()
    {
        // For now, just a message or open dashboard url
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://localhost:5191") { UseShellExecute = true });
    }

    [RelayCommand]
    private void Settings()
    {
        try
        {
            var settingsWindow = new SettingsWindow(_settingsService, _apiClient, _scheduler);
            settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            settingsWindow.ShowDialog();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ouverture des paramètres:\n{ex.Message}", "WellBot", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ChangeAvatar()
    {
        try
        {
            // Re-open AvatarSelectionWindow directly at Step 2
            var avatarVm = new AvatarSelectionViewModel();
            avatarVm.Initialize(_settingsService.GetUserName(), startStep: 2);
            avatarVm.SelectedAvatar = _settingsService.GetAvatar();

            var avatarWindow = new WellBot.Desktop.Views.AvatarSelectionWindow(_settingsService);
            avatarWindow.DataContext = avatarVm;

            bool closing = false;
            avatarVm.OnConfirmed += () =>
            {
                _settingsService.SetAvatar(avatarVm.SelectedAvatar);
                _settingsService.SetUserName(avatarVm.UserName);
                if (!closing) { closing = true; avatarWindow.Close(); }
            };
            avatarVm.OnClosed += () =>
            {
                if (!closing) { closing = true; avatarWindow.Close(); }
            };

            avatarWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            avatarWindow.ShowDialog();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Erreur lors du changement d'avatar:\n{ex.Message}", "WellBot", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    [ObservableProperty]
    private System.Windows.FlowDirection _flowDirection = System.Windows.FlowDirection.LeftToRight;
}
