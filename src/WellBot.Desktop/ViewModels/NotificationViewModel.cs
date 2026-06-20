using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using WellBot.Shared.Enums;
using WellBot.Desktop.Resources;

namespace WellBot.Desktop.ViewModels;

public partial class NotificationViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private AvatarType _selectedAvatar;

    [ObservableProperty]
    private string _animationName = "idle";

    [ObservableProperty]
    private string _customPath = string.Empty;

    [ObservableProperty]
    private double _targetTop;

    [ObservableProperty]
    private double _startTop;

    public NotificationType Type { get; set; }

    public string AcknowledgeText => AppResources.IUnderstand;

    public event Action? OnRequestClose;
    public event Action? OnAcknowledged;
    public event Action? OnDismissed;

    [RelayCommand]
    private void Acknowledge()
    {
        OnAcknowledged?.Invoke();
        OnRequestClose?.Invoke();
    }

    [RelayCommand]
    private void Dismiss()
    {
        OnDismissed?.Invoke();
        OnRequestClose?.Invoke();
    }
}
