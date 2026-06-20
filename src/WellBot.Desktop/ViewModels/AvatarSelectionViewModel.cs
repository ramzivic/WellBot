using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using WellBot.Shared.Enums;

namespace WellBot.Desktop.ViewModels;

public partial class AvatarSelectionViewModel : BaseViewModel
{
    [ObservableProperty]
    private AvatarType _selectedAvatar = AvatarType.Bot1;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private int _currentStep = 1; // 1: Welcome/Intro, 2: Selection

    public event Action? OnConfirmed;
    public event Action? OnClosed;

    public void Initialize(string initialName, int startStep = 1)
    {
        UserName = initialName;
        CurrentStep = startStep;
    }

    [RelayCommand]
    private void NextStep()
    {
        CurrentStep = 2;
    }

    [RelayCommand]
    private void SelectAvatar(string botIndex)
    {
        SelectedAvatar = botIndex switch
        {
            "1" => AvatarType.Bot1,
            "2" => AvatarType.Bot2,
            "3" => AvatarType.Bot3,
            "4" => AvatarType.Bot4,
            _ => AvatarType.Bot1
        };
    }

    [RelayCommand]
    private void Confirm()
    {
        OnConfirmed?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        OnClosed?.Invoke();
    }
}
