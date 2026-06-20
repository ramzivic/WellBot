using System.IO;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WellBot.Shared.Enums;

namespace WellBot.Desktop.Controls;

public partial class LottieAvatarControl : UserControl
{
    public static readonly DependencyProperty AvatarTypeProperty =
        DependencyProperty.Register(nameof(AvatarType), typeof(AvatarType), typeof(LottieAvatarControl), new PropertyMetadata(AvatarType.Bot1, OnAvatarChanged));

    public static readonly DependencyProperty AnimationNameProperty =
        DependencyProperty.Register(nameof(AnimationName), typeof(string), typeof(LottieAvatarControl), new PropertyMetadata("idle", OnAvatarChanged));

    public static readonly DependencyProperty CustomPathProperty =
        DependencyProperty.Register(nameof(CustomPath), typeof(string), typeof(LottieAvatarControl), new PropertyMetadata(string.Empty, OnAvatarChanged));

    private Storyboard? _activeStoryboard;

    public AvatarType AvatarType
    {
        get => (AvatarType)GetValue(AvatarTypeProperty);
        set => SetValue(AvatarTypeProperty, value);
    }

    public string AnimationName
    {
        get => (string)GetValue(AnimationNameProperty);
        set => SetValue(AnimationNameProperty, value);
    }

    public string CustomPath
    {
        get => (string)GetValue(CustomPathProperty);
        set => SetValue(CustomPathProperty, value);
    }

    public LottieAvatarControl()
    {
        InitializeComponent();
        UpdateAvatar();
    }

    private static void OnAvatarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LottieAvatarControl control)
        {
            control.UpdateAvatar();
        }
    }

    private void UpdateAvatar()
    {
        StopAnimation();

        if (AvatarType == AvatarType.Custom)
        {
            if (!string.IsNullOrEmpty(CustomPath) && File.Exists(CustomPath))
            {
                try { AvatarImage.Source = new BitmapImage(new Uri(CustomPath)); } catch { }
            }
            WaterGlassCanvas.Opacity = 0;
            return;
        }

        // Always load the user's selected avatar head
        LoadAvatarHead();

        // Start drink animation if needed
        if (AnimationName == "drink")
        {
            StartDrinkAnimation();
        }
        else
        {
            WaterGlassCanvas.Opacity = 0;
        }
    }

    private string GetGenderFolder()
    {
        if (AvatarType == AvatarType.Doctor)
        {
            // We need to know the base avatar to determine the doctor's gender.
            // Since LottieAvatarControl doesn't know the base avatar natively, we'll check the current settings if possible.
            try
            {
                var settings = ((WellBot.Desktop.App)System.Windows.Application.Current).ServiceProvider.GetService(typeof(WellBot.Desktop.Services.ISettingsService)) as WellBot.Desktop.Services.ISettingsService;
                if (settings != null && settings.GetAvatar() == AvatarType.Bot2)
                {
                    return "DoctorFemale";
                }
            }
            catch { }
            return "Doctor";
        }
        return (AvatarType == AvatarType.Bot2) ? "Female" : "Male";
    }

    /// <summary>
    /// Loads the user's selected avatar head image (idle.png).
    /// </summary>
    private void LoadAvatarHead()
    {
        string pose = AnimationName;
        if (pose != "idle" && pose != "stretch")
        {
            pose = "idle"; // For drink animation, we keep idle head and animate it
        }
        SetPose(pose);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BitmapImage> _imageCache = new();

    /// <summary>
    /// Changes the avatar image to a specific pose, using an in-memory cache to prevent repeated disk I/O.
    /// </summary>
    public void SetPose(string poseName)
    {
        string gender = GetGenderFolder();
        string cacheKey = $"{gender}_{poseName}";

        if (_imageCache.TryGetValue(cacheKey, out var cachedImage))
        {
            AvatarImage.Source = cachedImage;
            return;
        }

        try
        {
            string uri = $"pack://application:,,,/Assets/Avatars/{gender}/{poseName}.png";
            var bitmap = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
            _imageCache[cacheKey] = bitmap;
            AvatarImage.Source = bitmap;
        }
        catch
        {
            try
            {
                string fallbackKey = $"{gender}_idle";
                if (_imageCache.TryGetValue(fallbackKey, out var fallbackCached))
                {
                    AvatarImage.Source = fallbackCached;
                    return;
                }

                string uri = $"pack://application:,,,/Assets/Avatars/{gender}/idle.png";
                var fallbackBitmap = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
                _imageCache[fallbackKey] = fallbackBitmap;
                AvatarImage.Source = fallbackBitmap;
            }
            catch { /* Ignore */ }
        }
    }

    /// <summary>
    /// Starts the drinking animation using pure WPF transforms:
    /// - Head tilts back (simulating drinking motion)
    /// - Water glass rises from below to the mouth area
    /// - Subtle head bounce for liveliness
    /// - Loops continuously while notification is visible
    /// </summary>
    public void StartDrinkAnimation()
    {
        var storyboard = new Storyboard
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        var totalDuration = TimeSpan.FromSeconds(4.0);

        // ============================================
        // 1. HEAD TILT BACK (simulating drinking)
        // ============================================
        // Sequence: neutral → tilt back → hold → return to neutral → pause
        var headTilt = new DoubleAnimationUsingKeyFrames();
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));        // Neutral
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5)))       // Start tilting
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)))      // Full tilt (drinking)
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-18, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)))      // Gulping
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0)))      // Still drinking
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8)))       // Lowering
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.3)))        // Back to neutral
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(totalDuration)));                   // Pause

        Storyboard.SetTarget(headTilt, AvatarImage);
        Storyboard.SetTargetProperty(headTilt, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)"));
        storyboard.Children.Add(headTilt);

        // ============================================
        // 2. HEAD SUBTLE VERTICAL BOUNCE
        // ============================================
        var headBounce = new DoubleAnimationUsingKeyFrames();
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(-6, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.3)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        headBounce.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(totalDuration)));

        Storyboard.SetTarget(headBounce, AvatarImage);
        Storyboard.SetTargetProperty(headBounce, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));
        storyboard.Children.Add(headBounce);

        // ============================================
        // 3. WATER GLASS - OPACITY (appear/disappear)
        // ============================================
        var glassOpacity = new DoubleAnimationUsingKeyFrames();
        glassOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        glassOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        glassOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8))));
        glassOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.3)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        glassOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(totalDuration)));

        Storyboard.SetTarget(glassOpacity, WaterGlassCanvas);
        Storyboard.SetTargetProperty(glassOpacity, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(glassOpacity);

        // ============================================
        // 4. WATER GLASS - VERTICAL MOVEMENT (rises up to mouth)
        // ============================================
        var glassMove = new DoubleAnimationUsingKeyFrames();
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(20, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));      // Start below
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5)))       // Rise to face level
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)))     // Up to mouth
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(-20, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)))     // Tilt up (drinking)
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0)))     // Still drinking
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8)))       // Lower glass
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(20, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.3)))      // Back down
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });
        glassMove.KeyFrames.Add(new EasingDoubleKeyFrame(20, KeyTime.FromTimeSpan(totalDuration)));

        Storyboard.SetTarget(glassMove, WaterGlassCanvas);
        Storyboard.SetTargetProperty(glassMove, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));
        storyboard.Children.Add(glassMove);

        // ============================================
        // 5. WATER GLASS - TILT (tilts when drinking)
        // ============================================
        var glassTilt = new DoubleAnimationUsingKeyFrames();
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8))));
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-30, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2)))     // Tilt glass
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-40, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)))     // More tilt (pouring)
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-30, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8)))       // Upright again
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        glassTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(totalDuration)));

        Storyboard.SetTarget(glassTilt, WaterGlassCanvas);
        Storyboard.SetTargetProperty(glassTilt, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)"));
        storyboard.Children.Add(glassTilt);

        _activeStoryboard = storyboard;
        storyboard.Begin(this, true);
    }

    public void StopAnimation()
    {
        if (_activeStoryboard != null)
        {
            _activeStoryboard.Stop(this);
            _activeStoryboard = null;
        }

        // Reset all transforms
        AvatarRotation.Angle = 0;
        AvatarTranslation.Y = 0;
        GlassTranslation.X = 0;
        GlassTranslation.Y = 0;
        GlassRotation.Angle = 0;
        WaterGlassCanvas.Opacity = 0;
    }
}
