using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WellBot.Shared.Enums;

namespace WellBot.Desktop.Views;

public partial class VisualBreakWindow : Window
{
    private Storyboard? _mainStoryboard;
    private Storyboard? _extraStoryboard;
    private DispatcherTimer? _autoCloseTimer;
    private readonly List<DispatcherTimer> _activeTimers = new();
    private bool _isClosed = false;

    public NotificationType Type { get; set; } = NotificationType.VisualBreak;
    public AvatarType SelectedAvatar { get; set; }
    public string CustomAvatarPath { get; set; } = string.Empty;
    public WellBot.Shared.DTOs.AnimationSettingsDto AnimationSettings { get; set; } = new();

    public event Action? OnAcknowledged;
    public event Action? OnDismissed;

    public VisualBreakWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        StopAllAnimations();
    }

    private void StopAllAnimations()
    {
        try { _autoCloseTimer?.Stop(); } catch { }
        try { _mainStoryboard?.Stop(this); } catch { }
        try { _extraStoryboard?.Stop(this); } catch { }

        foreach (var timer in _activeTimers)
        {
            try { timer.Stop(); } catch { }
        }
        _activeTimers.Clear();

        // Stop avatar drink animation if running
        try { AvatarControl?.StopAnimation(); } catch { }
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Setup Avatar Control
        AvatarControl.AvatarType = SelectedAvatar;
        AvatarControl.CustomPath = CustomAvatarPath;

        StartFullAnimation();

        // Auto-close after 20 seconds
        _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _autoCloseTimer.Tick += (s, ev) =>
        {
            _autoCloseTimer.Stop();
            if (_isClosed) return;
            OnDismissed?.Invoke();
            CloseWithSlideDown();
        };
        _autoCloseTimer.Start();
    }

    private void StartFullAnimation()
    {
        _mainStoryboard = new Storyboard();

        // 1. AVATAR SLIDE UP FROM TASKBAR
        var slideUp = new DoubleAnimationUsingKeyFrames();
        slideUp.KeyFrames.Add(new EasingDoubleKeyFrame(180, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        slideUp.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0)))
        { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 } });
        slideUp.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(15))));
        slideUp.KeyFrames.Add(new EasingDoubleKeyFrame(180, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(16)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } });

        // If stretching or walking, prepare the avatar pose and scale immediately
        if (Type == NotificationType.Stretching)
        {
            AvatarControl.SetPose("stretchC");
            AvatarScale.ScaleX = AnimationSettings.StretchingScale;
            AvatarScale.ScaleY = AnimationSettings.StretchingScale;
        }
        else if (Type == NotificationType.ActiveBreak)
        {
            AvatarControl.SetPose("walk1");
            AvatarScale.ScaleX = AnimationSettings.ActiveBreakScale;
            AvatarScale.ScaleY = AnimationSettings.ActiveBreakScale;
        }

        Storyboard.SetTarget(slideUp, AvatarContainer);
        Storyboard.SetTargetProperty(slideUp, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
        _mainStoryboard.Children.Add(slideUp);

        // 2. SPEECH BUBBLE FADE IN/OUT
        var bubbleOpacity = new DoubleAnimationUsingKeyFrames();
        bubbleOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        bubbleOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        bubbleOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5))));
        bubbleOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(14))));
        bubbleOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(15))));
        Storyboard.SetTarget(bubbleOpacity, SpeechBubble);
        Storyboard.SetTargetProperty(bubbleOpacity, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(bubbleOpacity);

        // 2b. CLOSE BUTTON FADE IN/OUT
        var closeBtnOpacity = new DoubleAnimationUsingKeyFrames();
        closeBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        closeBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        closeBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5))));
        closeBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(14))));
        closeBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(15))));
        Storyboard.SetTarget(closeBtnOpacity, CloseBtn);
        Storyboard.SetTargetProperty(closeBtnOpacity, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(closeBtnOpacity);

        // 2c. ACKNOWLEDGE BUTTON FADE IN/OUT (now separate from speech bubble)
        var ackBtnOpacity = new DoubleAnimationUsingKeyFrames();
        ackBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        ackBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        ackBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5))));
        ackBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(14))));
        ackBtnOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(15))));
        Storyboard.SetTarget(ackBtnOpacity, AcknowledgeBtn);
        Storyboard.SetTargetProperty(ackBtnOpacity, new PropertyPath(OpacityProperty));
        _mainStoryboard.Children.Add(ackBtnOpacity);

        _mainStoryboard.Completed += (s, ev) =>
        {
            if (_isClosed) return;
            try { Close(); } catch { }
        };
        _mainStoryboard.Begin(this, true);

        // 3. SPECIFIC ANIMATIONS BASED ON TYPE
        if (Type == NotificationType.VisualBreak)
            StartVisualBreakSpecificAnimations();
        else if (Type == NotificationType.Hydration)
            StartHydrationSpecificAnimations();
        else if (Type == NotificationType.Breathing)
            StartBreathingSpecificAnimations();
        else if (Type == NotificationType.Stretching)
            StartStretchingSpecificAnimations();
        else if (Type == NotificationType.ActiveBreak)
            StartActiveBreakSpecificAnimations();
    }

    private void StartVisualBreakSpecificAnimations()
    {
        double startDelay = 2.0;

        // Subtle horizontal shift using a storyboard
        _extraStoryboard = new Storyboard();
        var headShiftAnim = new DoubleAnimationUsingKeyFrames();
        headShiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay))));
        headShiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 1)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        headShiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(12))));
        headShiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(13)))
        { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } });
        Storyboard.SetTargetName(headShiftAnim, "HeadTranslation");
        Storyboard.SetTargetProperty(headShiftAnim, new PropertyPath(TranslateTransform.XProperty));
        _extraStoryboard.Children.Add(headShiftAnim);
        _extraStoryboard.Begin(this);

        // Pose sequence using a single chained timer approach (no flood of timers)
        var sequence = new (string pose, double holdSeconds)[]
        {
            ("look_away", 2.5),
            ("eyes_closed", 0.3),
            ("look_away", 2.0),
            ("eyes_closed", 0.3),
            ("look_away", 2.0),
            ("eyes_closed", 0.3),
            ("look_away", 1.5),
            ("eyes_closed", 0.3),
            ("look_away", 1.0),
            ("idle", 0),
        };

        int step = 0;
        var poseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(startDelay) };
        _activeTimers.Add(poseTimer);

        poseTimer.Tick += (s, e) =>
        {
            poseTimer.Stop();
            if (_isClosed || !IsLoaded) return;
            if (step >= sequence.Length) return;

            var (pose, holdSeconds) = sequence[step];
            try { AvatarControl.SetPose(pose); } catch { }
            step++;

            if (step < sequence.Length && holdSeconds > 0)
            {
                poseTimer.Interval = TimeSpan.FromSeconds(holdSeconds);
                poseTimer.Start();
            }
        };
        poseTimer.Start();
    }

    private void StartHydrationSpecificAnimations()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.0) };
        _activeTimers.Add(timer);
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            if (_isClosed || !IsLoaded) return;
            try { AvatarControl.StartDrinkAnimation(); } catch { }
        };
        timer.Start();
    }

    private void StartBreathingSpecificAnimations()
    {
        double inhaleTime = 4.0;
        double exhaleTime = 4.0;
        double cycleTime = inhaleTime + exhaleTime;
        int cycles = 3;
        double startDelay = AnimationSettings.DefaultStartDelay;

        _extraStoryboard = new Storyboard();

        var scaleXAnim = new DoubleAnimationUsingKeyFrames();
        var scaleYAnim = new DoubleAnimationUsingKeyFrames();
        var headTiltAnim = new DoubleAnimationUsingKeyFrames();

        double t = startDelay;
        scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t))));
        scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t))));
        headTiltAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t))));

        for (int i = 0; i < cycles; i++)
        {
            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + inhaleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + inhaleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            headTiltAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + inhaleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });

            scaleXAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + cycleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            scaleYAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + cycleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            headTiltAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t + cycleTime)))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });

            t += cycleTime;
        }

        Storyboard.SetTargetName(scaleXAnim, "AvatarScale");
        Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        _extraStoryboard.Children.Add(scaleXAnim);

        Storyboard.SetTargetName(scaleYAnim, "AvatarScale");
        Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
        _extraStoryboard.Children.Add(scaleYAnim);

        Storyboard.SetTargetName(headTiltAnim, "HeadRotation");
        Storyboard.SetTargetProperty(headTiltAnim, new PropertyPath(RotateTransform.AngleProperty));
        _extraStoryboard.Children.Add(headTiltAnim);
        _extraStoryboard.Begin(this);

        // Image swap via single chained timer
        int phase = 0;
        int totalPhases = cycles * 2;
        var guideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(startDelay) };
        _activeTimers.Add(guideTimer);

        guideTimer.Tick += (s, e) =>
        {
            guideTimer.Stop();
            if (_isClosed || !IsLoaded) return;
            if (phase >= totalPhases)
            {
                try { AvatarControl.SetPose("idle"); } catch { }
                return;
            }

            bool isInhale = (phase % 2 == 0);
            try { AvatarControl.SetPose(isInhale ? "breathe_in" : "breathe_out"); } catch { }
            phase++;
            guideTimer.Interval = TimeSpan.FromSeconds(isInhale ? inhaleTime : exhaleTime);
            guideTimer.Start();
        };
        guideTimer.Start();
    }

    private void StartStretchingSpecificAnimations()
    {
        double startDelay = AnimationSettings.DefaultStartDelay;
        _extraStoryboard = new Storyboard();

        var shiftAnim = new DoubleAnimationUsingKeyFrames();
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay))));
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 1.5)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 3.5))));
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 5.5)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 7.5))));
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 9.5)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 11.5))));
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 13.5)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 15.5))));
        shiftAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startDelay + 17)))
        { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        Storyboard.SetTargetName(shiftAnim, "HeadTranslation");
        Storyboard.SetTargetProperty(shiftAnim, new PropertyPath(TranslateTransform.XProperty));
        _extraStoryboard.Children.Add(shiftAnim);

        var scaleAnim = new DoubleAnimationUsingKeyFrames();
        scaleAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(3.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        scaleAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(3.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(17.0))));
        var scaleAnimY = scaleAnim.Clone();
        Storyboard.SetTargetName(scaleAnim, "AvatarScale");
        Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
        _extraStoryboard.Children.Add(scaleAnim);
        Storyboard.SetTargetName(scaleAnimY, "AvatarScale");
        Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath(ScaleTransform.ScaleYProperty));
        _extraStoryboard.Children.Add(scaleAnimY);

        var resetRot = new DoubleAnimationUsingKeyFrames();
        resetRot.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        Storyboard.SetTargetName(resetRot, "HeadRotation");
        Storyboard.SetTargetProperty(resetRot, new PropertyPath(RotateTransform.AngleProperty));
        _extraStoryboard.Children.Add(resetRot);
        _extraStoryboard.Begin(this);

        // Use chained timers instead of many independent ones
        var stretchSequence = new (string pose, double atSeconds)[]
        {
            ("stretchL", startDelay + 1.0),
            ("stretchC", startDelay + 4.5),
            ("stretchR", startDelay + 5.5),
            ("stretchC", startDelay + 8.5),
            ("stretchL", startDelay + 9.5),
            ("stretchC", startDelay + 12.5),
            ("stretchR", startDelay + 13.5),
            ("stretchC", startDelay + 16.0),
            ("idle",    startDelay + 17.5),
        };

        // Chained timer cycling through ALL poses
        int step = 0;
        var poseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(stretchSequence[0].atSeconds) };
        _activeTimers.Add(poseTimer);
        poseTimer.Tick += (s, e) =>
        {
            poseTimer.Stop();
            if (_isClosed || !IsLoaded) return;
            if (step >= stretchSequence.Length) return;

            try { AvatarControl.SetPose(stretchSequence[step].pose); } catch { }
            step++;

            if (step < stretchSequence.Length)
            {
                double nextDelay = stretchSequence[step].atSeconds - stretchSequence[step - 1].atSeconds;
                if (nextDelay < 0.05) nextDelay = 0.05;
                poseTimer.Interval = TimeSpan.FromSeconds(nextDelay);
                poseTimer.Start();
            }
        };
        poseTimer.Start();
    }

    private void StartActiveBreakSpecificAnimations()
    {
        double startDelay = AnimationSettings.DefaultStartDelay;
        double poseInterval = 0.4;
        string[] poses = { "walk1", "walk2", "walk3" };
        HeadTranslation.X = 0;

        // Use a single repeating timer instead of ~37 individual ones
        int poseIndex = 0;
        int totalSteps = (int)((15.5 - startDelay) / poseInterval);
        int currentStep = 0;

        var startTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(startDelay) };
        _activeTimers.Add(startTimer);
        startTimer.Tick += (s, e) =>
        {
            startTimer.Stop();
            if (_isClosed || !IsLoaded) return;

            var walkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(poseInterval) };
            _activeTimers.Add(walkTimer);
            walkTimer.Tick += (s2, e2) =>
            {
                if (_isClosed || !IsLoaded)
                {
                    walkTimer.Stop();
                    return;
                }
                if (currentStep >= totalSteps)
                {
                    walkTimer.Stop();
                    _activeTimers.Remove(walkTimer);
                    try { AvatarControl.SetPose("idle"); } catch { }
                    return;
                }
                try { AvatarControl.SetPose(poses[poseIndex % poses.Length]); } catch { }
                poseIndex++;
                currentStep++;
            };
            walkTimer.Start();
        };
        startTimer.Start();
    }

    private void OnAcknowledgeClick(object sender, RoutedEventArgs e)
    {
        OnAcknowledged?.Invoke();
        CloseWithSlideDown();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        OnDismissed?.Invoke();
        CloseWithSlideDown();
    }

    private void CloseWithSlideDown()
    {
        if (_isClosed) return;
        _isClosed = true;

        // Stop all timers and storyboards except main (we'll stop it manually)
        _autoCloseTimer?.Stop();
        foreach (var timer in _activeTimers)
        {
            try { timer.Stop(); } catch { }
        }
        _activeTimers.Clear();

        try { _extraStoryboard?.Stop(this); } catch { }
        try { _mainStoryboard?.Stop(this); } catch { }
        try { AvatarControl?.StopAnimation(); } catch { }

        var slideDown = new DoubleAnimation(AvatarSlideTransform.Y, 180, TimeSpan.FromSeconds(0.6))
        {
            EasingFunction = new QuadraticEase()
        };
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
        try
        {
            SpeechBubble.BeginAnimation(OpacityProperty, fadeOut);
            CloseBtn.BeginAnimation(OpacityProperty, fadeOut.Clone());
            AcknowledgeBtn.BeginAnimation(OpacityProperty, fadeOut.Clone());
            slideDown.Completed += (s, e) =>
            {
                try { Close(); } catch { }
            };
            AvatarSlideTransform.BeginAnimation(TranslateTransform.YProperty, slideDown);
        }
        catch
        {
            try { Close(); } catch { }
        }
    }
}
