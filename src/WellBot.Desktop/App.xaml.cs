using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WellBot.Desktop.ViewModels;
using WellBot.Desktop.Views;
using WellBot.Desktop.Services;
using WellBot.Shared.DTOs;
using WellBot.Shared.Enums;
using System.Threading;
using System.Windows.Threading;

namespace WellBot.Desktop;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;
    private const string MutexName = "WellBot_SingleInstance_Mutex";

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {


        this.DispatcherUnhandledException += (s, args) =>
        {
            LogExceptionToEventViewer($"DispatcherUnhandledException: {args.Exception}");
            System.IO.File.WriteAllText("crash.log", args.Exception.ToString());
            MessageBox.Show(args.Exception.Message, "Crash");
            args.Handled = true;
        };
        
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            LogExceptionToEventViewer($"AppDomain UnhandledException: {args.ExceptionObject}");
            System.IO.File.WriteAllText("crash2.log", args.ExceptionObject.ToString());
        };

        // Single Instance check
        _mutex = new Mutex(true, MutexName, out _ownsMutex);
        if (!_ownsMutex)
        {
            MessageBox.Show("WellBot is already running.", "WellBot", MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            StartAppAsync();
        }
        catch (Exception ex)
        {
            LogExceptionToEventViewer($"OnStartup Exception: {ex}");
            System.IO.File.WriteAllText("crash3.log", ex.ToString());
            MessageBox.Show(ex.ToString(), "Crash in OnStartup");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<NotificationViewModel>();
        services.AddTransient<AvatarSelectionViewModel>();

        // Views
        services.AddSingleton<MainWindow>(s => new MainWindow
        {
            DataContext = s.GetRequiredService<MainViewModel>()
        });
        services.AddTransient<NotificationPopupWindow>();
        services.AddTransient<AvatarSelectionWindow>();

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddHttpClient<IWellBotApiClient, WellBotApiClient>((sp, client) => 
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            client.BaseAddress = new Uri(settings.GetServerUrl());
            var authBytes = System.Text.Encoding.ASCII.GetBytes($"{settings.GetServerUsername()}:{settings.GetServerPassword()}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        });
        
        services.AddSingleton<IDndDetectorService, DndDetectorService>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<ITextToSpeechService, TextToSpeechService>();
        services.AddSingleton<INotificationScheduler>(s => 
            new NotificationScheduler(config => ShowNotification(config, s)));
    }

    private async void StartAppAsync()
    {
        var settings = ServiceProvider.GetRequiredService<ISettingsService>();
        var apiClient = ServiceProvider.GetRequiredService<IWellBotApiClient>();
        var scheduler = ServiceProvider.GetRequiredService<INotificationScheduler>();
        var analytics = ServiceProvider.GetRequiredService<IAnalyticsService>();

        analytics.Start();

        // Listen for language changes to dynamically fetch and update localized break notifications
        settings.LanguageChanged += async () =>
        {
            try
            {
                var newLang = settings.GetLanguage();
                var newConfig = await apiClient.GetConfigAsync(newLang);
                if (newConfig != null)
                {
                    await settings.SaveConfigCacheAsync(newConfig);
                    scheduler.UpdateConfig(newConfig.Notifications);
                }
                else
                {
                    var cachedConfig = await settings.LoadConfigCacheAsync();
                    if (cachedConfig != null && cachedConfig.Notifications?.FirstOrDefault()?.Language == newLang)
                    {
                        scheduler.UpdateConfig(cachedConfig.Notifications);
                    }
                    else
                    {
                        scheduler.UpdateConfig(GetFallbackConfigs(newLang, settings));
                    }
                }

                // Close active notifications so they don't linger in the old language
                lock (_activeNotifications)
                {
                    foreach (var win in _activeNotifications.ToList())
                    {
                        try { win.Close(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogExceptionToEventViewer($"Error updating config on language change: {ex}");
                System.Diagnostics.Debug.WriteLine($"Error updating config on language change: {ex}");
            }
        };

        var language = settings.GetLanguage();

        if (settings.IsFirstLaunch())
        {
            var avatarVm = ServiceProvider.GetRequiredService<AvatarSelectionViewModel>();
            avatarVm.Initialize(settings.GetUserName());
            
            var avatarWindow = new AvatarSelectionWindow(settings) { DataContext = avatarVm };
            
            avatarVm.OnConfirmed += () =>
            {
                settings.SetAvatar(avatarVm.SelectedAvatar);
                settings.SetUserName(avatarVm.UserName);
                settings.SetFirstLaunch(false);
                avatarWindow.Close();
            };

            avatarVm.OnClosed += () => 
            {
                avatarWindow.Close();
                Shutdown();
            };

            avatarWindow.ShowDialog();
        }

        var config = await apiClient.GetConfigAsync(language);

        if (config != null)
        {
            await settings.SaveConfigCacheAsync(config);
            var localAnimSettings = settings.GetAnimationSettings();
            if (!localAnimSettings.OverrideServerSettings && config.AnimationSettings != null)
            {
                settings.SetAnimationSettings(config.AnimationSettings);
            }
            scheduler.UpdateConfig(config.Notifications);
        }
        else
        {
            var cachedConfig = await settings.LoadConfigCacheAsync();
            if (cachedConfig != null && cachedConfig.Notifications?.FirstOrDefault()?.Language == language)
            {
                scheduler.UpdateConfig(cachedConfig.Notifications);
            }
            else
            {
                scheduler.UpdateConfig(GetFallbackConfigs(language, settings));
            }
        }
        
        var dndService = ServiceProvider.GetRequiredService<IDndDetectorService>();
        dndService.Start();

        dndService.OnSessionUnlocked += () =>
        {
            var missed = dndService.GetMissedNotifications().ToList();
            dndService.ClearMissedNotifications();
            foreach (var m in missed)
            {
                // Call ShowNotification directly. They will be stacked horizontally due to GetNextHorizontalPosition
                ShowNotification(m, ServiceProvider);
            }
        };

        var autoStart = ServiceProvider.GetRequiredService<IAutoStartService>();
        autoStart.Enable();

        scheduler.Start();

        // Auto-sync every 5 minutes (was 60) to catch web config changes faster
        var syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        syncTimer.Tick += async (s, e) => 
        {
            var newConfig = await apiClient.GetConfigAsync(settings.GetLanguage());
            if (newConfig != null)
            {
                var localAnimSettings = settings.GetAnimationSettings();
                if (!localAnimSettings.OverrideServerSettings && newConfig.AnimationSettings != null)
                {
                    settings.SetAnimationSettings(newConfig.AnimationSettings);
                }

                await settings.SaveConfigCacheAsync(newConfig);
                scheduler.UpdateConfig(newConfig.Notifications);
            }
        };
        syncTimer.Start();
    }

    private static readonly List<Window> _activeNotifications = new();

    private async void ShowNotification(NotificationConfigDto config, IServiceProvider serviceProvider)
    {
        try 
        {
            var settings = serviceProvider.GetRequiredService<ISettingsService>();
            if (!settings.IsWithinWorkingHours()) return;

            var dndService = serviceProvider.GetRequiredService<IDndDetectorService>();
            if (dndService.IsSessionLocked)
            {
                dndService.AddMissedNotification(config);
                return;
            }
            if (dndService.IsDndActive) return;

            var analytics = serviceProvider.GetRequiredService<IAnalyticsService>();
            analytics.TrackEvent(config.Type, AnalyticsAction.Displayed);

            string title = config.Title;
            string message = config.Message;

            if (config.Type == NotificationType.HealthTip)
            {
                if (!config.IsEnabled)
                {
                    var cachedConfig = await settings.LoadConfigCacheAsync();
                    if (cachedConfig != null && cachedConfig.HealthTips != null && cachedConfig.HealthTips.Count > 0)
                    {
                        var random = new Random();
                        var randomTip = cachedConfig.HealthTips[random.Next(cachedConfig.HealthTips.Count)];
                        title = randomTip.Title;
                        message = randomTip.Message;
                    }
                    else
                    {
                        title = "Conseil Santé 🏥";
                        message = "Prenez soin de votre santé au travail !";
                    }
                }
            }

            // Use the pop-up avatar window for specific interactive animations
            if (config.Type == NotificationType.VisualBreak || 
                config.Type == NotificationType.Hydration || 
                config.Type == NotificationType.Breathing || 
                config.Type == NotificationType.Stretching ||
                config.Type == NotificationType.ActiveBreak)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => ShowVisualBreakNotification(config, settings, analytics));
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var viewModel = serviceProvider.GetRequiredService<NotificationViewModel>();
                    viewModel.Title = title;
                    viewModel.Message = message;
                    viewModel.AnimationName = config.AnimationName;
                    viewModel.Type = config.Type;
                    
                    if (config.Type == NotificationType.HealthTip)
                    {
                        viewModel.SelectedAvatar = AvatarType.Doctor;
                        viewModel.CustomPath = string.Empty;
                    }
                    else
                    {
                        viewModel.SelectedAvatar = settings.GetAvatar();
                        viewModel.CustomPath = settings.GetCustomAvatarPath();
                    }

                    var popup = new NotificationPopupWindow
                    {
                        DataContext = viewModel
                    };

                    // Position calculation for horizontal stacking
                    var workArea = SystemParameters.WorkArea;
                    popup.Left = GetNextHorizontalPosition(popup.Width);
                    popup.Opacity = 0;

                    RegisterNotification(popup);

                    viewModel.OnRequestClose += () => { try { popup.Close(); } catch { } };
                    viewModel.OnAcknowledged += () => analytics.TrackEvent(config.Type, AnalyticsAction.Acknowledged);
                    viewModel.OnDismissed += () => analytics.TrackEvent(config.Type, AnalyticsAction.Dismissed);
                    
                    popup.Loaded += (s, e) => 
                    {
                        popup.Top = workArea.Bottom - popup.ActualHeight - 10;

                        var fadeAnim = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
                        popup.BeginAnimation(Window.OpacityProperty, fadeAnim);

                        var slideAnim = new System.Windows.Media.Animation.DoubleAnimation(100, 0, TimeSpan.FromSeconds(0.5))
                        {
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                        };
                        popup.MainTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideAnim);
                    };

                    popup.Show();
                    PlaySoftNotificationSound();

                    // TTS : lire le message à voix haute si activé
                    if (settings.IsTtsEnabled())
                    {
                        var tts = serviceProvider.GetRequiredService<ITextToSpeechService>();
                        bool isFemale = settings.GetAvatar() == AvatarType.Bot2;
                        tts.SpeakAsync(message, settings.GetLanguage(), isFemale);
                    }

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
                    timer.Tick += (s, e) => { try { popup.Close(); } catch { } timer.Stop(); };
                    timer.Start();
                }
                catch (Exception ex)
                {
                    LogExceptionToEventViewer($"WellBot Error (UI thread): {ex}");
                    System.Diagnostics.Debug.WriteLine($"WellBot Error (UI thread): {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            LogExceptionToEventViewer($"WellBot Error: {ex}");
            System.Diagnostics.Debug.WriteLine($"WellBot Error: {ex}");
        }
    }

    private void ShowVisualBreakNotification(NotificationConfigDto config, ISettingsService settings, IAnalyticsService analytics)
    {
        var breakWindow = new VisualBreakWindow
        {
            Type = config.Type,
            SelectedAvatar = settings.GetAvatar(),
            CustomAvatarPath = settings.GetCustomAvatarPath(),
            AnimationSettings = settings.GetAnimationSettings(),
            DataContext = new { Title = config.Title, Message = config.Message, AcknowledgeText = WellBot.Desktop.Resources.AppResources.IUnderstand }
        };

        var workArea = SystemParameters.WorkArea;
        breakWindow.Left = GetNextHorizontalPosition(breakWindow.Width);
        breakWindow.Top = workArea.Bottom - breakWindow.Height;

        RegisterNotification(breakWindow);

        breakWindow.OnAcknowledged += () => analytics.TrackEvent(config.Type, AnalyticsAction.Acknowledged);
        breakWindow.OnDismissed += () => analytics.TrackEvent(config.Type, AnalyticsAction.Dismissed);

        breakWindow.Show();
        PlaySoftNotificationSound();

        // TTS : lire le message à voix haute si activé
        if (settings.IsTtsEnabled())
        {
            var tts = ServiceProvider.GetRequiredService<ITextToSpeechService>();
            bool isFemale = settings.GetAvatar() == AvatarType.Bot2;
            tts.SpeakAsync(config.Message, settings.GetLanguage(), isFemale);
        }
    }

    private void RegisterNotification(Window window)
    {
        lock (_activeNotifications)
        {
            _activeNotifications.Add(window);
        }
        window.Closed += (s, e) => 
        {
            lock (_activeNotifications)
            {
                _activeNotifications.Remove(window);
            }
        };
    }

    private double GetNextHorizontalPosition(double windowWidth)
    {
        var workArea = SystemParameters.WorkArea;
        double currentOffset = 10;

        lock (_activeNotifications)
        {
            foreach (var win in _activeNotifications)
            {
                currentOffset += win.Width + 10;
            }
        }

        return workArea.Right - currentOffset - windowWidth;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void PlaySoftNotificationSound()
    {
        try
        {
            // Un son doux et moderne de Windows
            var soundFile = @"C:\Windows\Media\Windows Notify Calendar.wav";
            if (System.IO.File.Exists(soundFile))
            {
                new System.Media.SoundPlayer(soundFile).Play();
            }
            else
            {
                // Fallback si le fichier n'existe pas
                System.Media.SystemSounds.Asterisk.Play();
            }
        }
        catch { }
    }

    private void LogExceptionToEventViewer(string message, System.Diagnostics.EventLogEntryType type = System.Diagnostics.EventLogEntryType.Error)
    {
        try
        {
            // Utilisation d'une source existante pour éviter de nécessiter des droits d'administrateur
            string source = "Application";
            string formattedMessage = $"[WellBot] {message}";
            System.Diagnostics.EventLog.WriteEntry(source, formattedMessage, type);
        }
        catch
        {
            // Fallback (ex: log plein)
            try
            {
                System.IO.File.AppendAllText("wellbot_error_fallback.log", $"{DateTime.Now}: {message}\n");
            }
            catch { }
        }
    }

    private static List<NotificationConfigDto> GetFallbackConfigs(string lang, ISettingsService settings)
    {
        var localSchedules = settings.GetLocalSchedules();
        var configs = new List<NotificationConfigDto>();

        void AddConfig(NotificationType type, string title, string message, string animation)
        {
            var schedule = localSchedules.FirstOrDefault(s => s.Type == type);
            if (schedule != null)
            {
                configs.Add(new NotificationConfigDto
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    IntervalMinutes = schedule.IntervalMinutes,
                    IsEnabled = schedule.IsEnabled,
                    AnimationName = animation,
                    Language = lang
                });
            }
        }

        if (lang == "en")
        {
            AddConfig(NotificationType.Hydration, "Hydration 💧", "Remember to drink a glass of water!", "drink");
            AddConfig(NotificationType.VisualBreak, "Visual Break 👀", "Look away from the screen to rest your eyes!", "look_away");
            AddConfig(NotificationType.Stretching, "Stretching 🤸", "Time to stretch! Try this exercise.", "stretch");
            AddConfig(NotificationType.ActiveBreak, "Active Break 🚶", "Take a short walk to relax.", "walk");
            AddConfig(NotificationType.Breathing, "Breathing 🧘", "Take a breathing break: inhale deeply and exhale slowly.", "breathe");
            AddConfig(NotificationType.HealthTip, "Health Tip 🏥", "Take care of your health at work!", "");
        }
        else if (lang == "ar")
        {
            AddConfig(NotificationType.Hydration, "💧 ترطيب", "!تذكر أن تشرب كوبًا من الماء", "drink");
            AddConfig(NotificationType.VisualBreak, "👀 راحة بصرية", "!انظر بعيدًا عن الشاشة لإراحة عينيك", "look_away");
            AddConfig(NotificationType.Stretching, "🤸 تمارين إطالة", ".حان وقت التمدد! جرب هذا التمرين", "stretch");
            AddConfig(NotificationType.ActiveBreak, "🚶 استراحة نشطة", ".قم بنزهة قصيرة للاسترخاء", "walk");
            AddConfig(NotificationType.Breathing, "🧘 تنفس", ".خذ استراحة تنفس: استنشق بعمق وازفر ببطء", "breathe");
            AddConfig(NotificationType.HealthTip, "🏥 نصيحة صحية", "!اعتن بصحتك في العمل", "");
        }
        else
        {
            // Default to fr
            AddConfig(NotificationType.Hydration, "Hydratation 💧", "Pensez à boire un verre d'eau !", "drink");
            AddConfig(NotificationType.VisualBreak, "Pause visuelle 👀", "Regardez au loin pour reposer vos yeux !", "look_away");
            AddConfig(NotificationType.Stretching, "Étirements 🤸", "Il est temps de vous étirer ! Essayez cet exercice.", "stretch");
            AddConfig(NotificationType.ActiveBreak, "Pause active 🚶", "Faites une petite promenade pour vous détendre.", "walk");
            AddConfig(NotificationType.Breathing, "Respiration 🧘", "Prenez une pause respiration : inspirez profondément et expirez lentement.", "breathe");
            AddConfig(NotificationType.HealthTip, "Conseil Santé 🏥", "Prenez soin de votre santé au travail !", "");
        }

        return configs;
    }
}
