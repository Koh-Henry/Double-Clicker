using System.Diagnostics;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

public sealed class ClickEngine : IDisposable
{
    private const int MaxPendingClicks = 32;
    private static readonly TimeSpan MaxInjectionLateness = TimeSpan.FromMilliseconds(40);

    private readonly AppSettings _settings;
    private readonly IInputInjector _inputInjector;
    private readonly IForegroundWindowService _foregroundWindowService;
    private readonly object _sync = new();
    private readonly PriorityQueue<ScheduledClick, long> _pendingClicks = new();
    private readonly Thread _workerThread;

    private PressSession _leftPress = new();
    private PressSession _rightPress = new();
    private volatile bool _isDisposed;

    public ClickEngine(
        AppSettings settings,
        IInputInjector inputInjector,
        IForegroundWindowService foregroundWindowService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _inputInjector = inputInjector ?? throw new ArgumentNullException(nameof(inputInjector));
        _foregroundWindowService = foregroundWindowService ?? throw new ArgumentNullException(nameof(foregroundWindowService));

        _workerThread = new Thread(ProcessScheduledClicks)
        {
            IsBackground = true,
            Name = "ClickEngineScheduler"
        };
        _workerThread.Start();
    }

    public event EventHandler<ClickInjectionFailedEventArgs>? InjectionFailed;

    public void HandleButtonDown(MouseButtonKind button)
    {
        ClickSettingsSnapshot settings = _settings.GetClickSettingsSnapshot();

        if (_isDisposed || !settings.IsEnabled || !settings.IsButtonEnabled(button))
        {
            return;
        }

        lock (_sync)
        {
            PressSession press = GetPressSession(button);
            press.IsTracking = true;
            press.DownTimeStamp = Stopwatch.GetTimestamp();
        }
    }

    public void HandleButtonUp(MouseButtonKind button)
    {
        long downTimestamp;
        bool wasTracking;

        lock (_sync)
        {
            PressSession press = GetPressSession(button);
            wasTracking = press.IsTracking;
            downTimestamp = press.DownTimeStamp;
            press.IsTracking = false;
            press.DownTimeStamp = 0;
        }

        ClickSettingsSnapshot settings = _settings.GetClickSettingsSnapshot();

        if (!wasTracking || _isDisposed || !settings.IsEnabled || !settings.IsButtonEnabled(button))
        {
            return;
        }

        if (settings.MinecraftOnly && !_foregroundWindowService.IsMinecraftFocused())
        {
            return;
        }

        TimeSpan elapsed = Stopwatch.GetElapsedTime(downTimestamp);

        if (elapsed.TotalMilliseconds > Math.Max(0, settings.TapMaxDurationMs))
        {
            return;
        }

        EnqueueScheduledClick(new ScheduledClick(
            button,
            GetDueTimestamp(settings.ExtraClickDelayMs)));
    }

    public void ClearPendingClicks()
    {
        lock (_sync)
        {
            _pendingClicks.Clear();
            _leftPress = new PressSession();
            _rightPress = new PressSession();
            Monitor.PulseAll(_sync);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _pendingClicks.Clear();
            _leftPress = new PressSession();
            _rightPress = new PressSession();
            Monitor.PulseAll(_sync);
        }

        _workerThread.Join();
    }

    private PressSession GetPressSession(MouseButtonKind button)
    {
        return button switch
        {
            MouseButtonKind.Left => _leftPress,
            MouseButtonKind.Right => _rightPress,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    }

    private void EnqueueScheduledClick(ScheduledClick scheduledClick)
    {
        lock (_sync)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_pendingClicks.Count >= MaxPendingClicks)
            {
                return;
            }

            _pendingClicks.Enqueue(scheduledClick, scheduledClick.DueTimestamp);
            Monitor.PulseAll(_sync);
        }
    }

    private void ProcessScheduledClicks()
    {
        while (true)
        {
            ScheduledClick? scheduledClick = WaitForNextClick();

            if (scheduledClick is null)
            {
                return;
            }

            ScheduledClick readyClick = scheduledClick.Value;
            ClickSettingsSnapshot settings = _settings.GetClickSettingsSnapshot();

            if (!settings.IsEnabled || !settings.IsButtonEnabled(readyClick.Button))
            {
                continue;
            }

            if (settings.MinecraftOnly && !_foregroundWindowService.IsMinecraftFocused())
            {
                continue;
            }

            if (Stopwatch.GetElapsedTime(readyClick.DueTimestamp) > MaxInjectionLateness)
            {
                continue;
            }

            try
            {
                _inputInjector.Click(readyClick.Button);
            }
            catch (Exception ex)
            {
                try
                {
                    InjectionFailed?.Invoke(this, new ClickInjectionFailedEventArgs(readyClick.Button, ex));
                }
                catch
                {
                    // A UI/status subscriber must not terminate the scheduler thread.
                }
            }
        }
    }

    private ScheduledClick? WaitForNextClick()
    {
        lock (_sync)
        {
            while (!_isDisposed)
            {
                if (_pendingClicks.Count == 0)
                {
                    Monitor.Wait(_sync);
                    continue;
                }

                _pendingClicks.TryPeek(out ScheduledClick nextClick, out _);
                long now = Stopwatch.GetTimestamp();

                if (nextClick.DueTimestamp > now)
                {
                    Monitor.Wait(_sync, GetRemainingDelay(nextClick.DueTimestamp, now));
                    continue;
                }

                return _pendingClicks.Dequeue();
            }

            return null;
        }
    }

    private static TimeSpan GetRemainingDelay(long dueTimestamp, long currentTimestamp)
    {
        long remainingTicks = dueTimestamp - currentTimestamp;
        return StopwatchTicksToTimeSpan(remainingTicks > 0 ? remainingTicks : 0);
    }

    private static long GetDueTimestamp(int extraClickDelayMs)
    {
        long delayTicks = extraClickDelayMs <= 0
            ? 0
            : (long)Math.Ceiling(extraClickDelayMs * (double)Stopwatch.Frequency / 1000d);

        return Stopwatch.GetTimestamp() + delayTicks;
    }

    private static TimeSpan StopwatchTicksToTimeSpan(long stopwatchTicks)
    {
        if (stopwatchTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        double seconds = stopwatchTicks / (double)Stopwatch.Frequency;
        return TimeSpan.FromSeconds(seconds);
    }

    private readonly record struct ScheduledClick(MouseButtonKind Button, long DueTimestamp);
}
