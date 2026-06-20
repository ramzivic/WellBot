using System.Windows;
using WellBot.Desktop.Services;

namespace WellBot.Desktop.Views;

public partial class AvatarSelectionWindow : Window
{
    private readonly ISettingsService? _settingsService;

    public AvatarSelectionWindow() : this(null) { }

    public AvatarSelectionWindow(ISettingsService? settingsService = null)
    {
        InitializeComponent();
        try
        {
            ImgBot.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/Icons/wellbot.png"));
            ImgLogo.Source = new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,,/Assets/Icons/ooredoo_logo.png"));
        }
        catch { }
        
        try
        {
            _settingsService = settingsService ?? ((WellBot.Desktop.App)System.Windows.Application.Current).ServiceProvider.GetService(typeof(ISettingsService)) as ISettingsService;
            if (_settingsService != null)
            {
                _settingsService.LanguageChanged += UpdateTexts;
            }
        }
        catch { }

        UpdateTexts();

        Closed += (s, e) =>
        {
            if (_settingsService != null)
            {
                _settingsService.LanguageChanged -= UpdateTexts;
            }
        };
    }

    private void UpdateTexts()
    {
        if (TxtWelcome1 != null)
        {
            var welcome = WellBot.Desktop.Resources.AppResources.AvatarWelcome;
            TxtWelcome1.Text = welcome.Replace("WellBot", "");
        }
        if (TxtSubWelcome != null) TxtSubWelcome.Text = WellBot.Desktop.Resources.AppResources.AvatarSubWelcome;
        if (BtnStartConfig != null) BtnStartConfig.Content = WellBot.Desktop.Resources.AppResources.AvatarStartConfig;
        if (TxtHello != null) TxtHello.Text = WellBot.Desktop.Resources.AppResources.AvatarHello;
        if (TxtLetsCustomize != null) TxtLetsCustomize.Text = WellBot.Desktop.Resources.AppResources.AvatarLetsCustomize;
        if (TxtNameQuestion != null) TxtNameQuestion.Text = WellBot.Desktop.Resources.AppResources.AvatarNameQuestion;
        if (TxtChooseAvatar != null) TxtChooseAvatar.Text = WellBot.Desktop.Resources.AppResources.AvatarChoose;
        if (BtnLetsGo != null) BtnLetsGo.Content = WellBot.Desktop.Resources.AppResources.AvatarLetsGo;
    }
}
