using System.Collections.Concurrent;
using MinecraftDoubleClicker.Models;
using MinecraftDoubleClicker.Services;

namespace MinecraftDoubleClicker.Tests;

public sealed class ClickEngineTests
{
    [Fact]
    public void QuickLeftTap_InjectsOneLeftClick()
    {
        AppSettings settings = CreateSettings();
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        Tap(engine, MouseButtonKind.Left);

        Assert.True(injector.WaitForCount(1));
        Assert.Equal([MouseButtonKind.Left], injector.Clicks);
    }

    [Fact]
    public void QuickRightTap_InjectsOneRightClickWhenEnabled()
    {
        AppSettings settings = CreateSettings();
        settings.RightClickEnabled = true;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        Tap(engine, MouseButtonKind.Right);

        Assert.True(injector.WaitForCount(1));
        Assert.Equal([MouseButtonKind.Right], injector.Clicks);
    }

    [Fact]
    public void RightTap_DoesNothingWhenRightClickIsDisabled()
    {
        AppSettings settings = CreateSettings();
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        Tap(engine, MouseButtonKind.Right);

        Assert.False(injector.WaitForCount(1, 100));
        Assert.Empty(injector.Clicks);
    }

    [Fact]
    public void LongPress_DoesNotInjectClick()
    {
        AppSettings settings = CreateSettings();
        settings.TapMaxDurationMs = 1;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        engine.HandleButtonDown(MouseButtonKind.Left);
        Thread.Sleep(20);
        engine.HandleButtonUp(MouseButtonKind.Left);

        Assert.False(injector.WaitForCount(1, 100));
    }

    [Fact]
    public void MinecraftOnly_DoesNotInjectWhenMinecraftIsNotFocused()
    {
        AppSettings settings = CreateSettings();
        settings.MinecraftOnly = true;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(false));

        Tap(engine, MouseButtonKind.Left);

        Assert.False(injector.WaitForCount(1, 100));
    }

    [Fact]
    public void ClearPendingClicks_CancelsDelayedClickAndActivePress()
    {
        AppSettings settings = CreateSettings();
        settings.ExtraClickDelayMs = 150;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        Tap(engine, MouseButtonKind.Left);
        engine.HandleButtonDown(MouseButtonKind.Left);
        engine.ClearPendingClicks();
        engine.HandleButtonUp(MouseButtonKind.Left);

        Assert.False(injector.WaitForCount(1, 250));
    }

    [Fact]
    public void LeftAndRightPresses_AreTrackedIndependently()
    {
        AppSettings settings = CreateSettings();
        settings.RightClickEnabled = true;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        engine.HandleButtonDown(MouseButtonKind.Left);
        engine.HandleButtonDown(MouseButtonKind.Right);
        engine.HandleButtonUp(MouseButtonKind.Left);
        engine.HandleButtonUp(MouseButtonKind.Right);

        Assert.True(injector.WaitForCount(2));
        Assert.Equal(2, injector.Clicks.Count);
        Assert.Contains(MouseButtonKind.Left, injector.Clicks);
        Assert.Contains(MouseButtonKind.Right, injector.Clicks);
    }

    [Fact]
    public void EarlierDueClick_IsNotBlockedByAnOlderDelayedClick()
    {
        AppSettings settings = CreateSettings();
        settings.RightClickEnabled = true;
        settings.ExtraClickDelayMs = 200;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        Tap(engine, MouseButtonKind.Left);
        settings.ExtraClickDelayMs = 0;
        Tap(engine, MouseButtonKind.Right);

        Assert.True(injector.WaitForCount(1, 100));
        Assert.Equal(MouseButtonKind.Right, injector.Clicks[0]);
        Assert.True(injector.WaitForCount(2, 300));
        Assert.Equal(MouseButtonKind.Left, injector.Clicks[1]);
    }

    [Fact]
    public void InjectionFailure_RaisesEventAndSchedulerContinues()
    {
        AppSettings settings = CreateSettings();
        FailOnceInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));
        using ManualResetEventSlim failed = new();
        engine.InjectionFailed += (_, args) =>
        {
            Assert.Equal(MouseButtonKind.Left, args.Button);
            failed.Set();
        };

        Tap(engine, MouseButtonKind.Left);
        Assert.True(failed.Wait(
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken));
        Tap(engine, MouseButtonKind.Left);

        Assert.True(injector.SuccessfulClick.Wait(
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken));
        Assert.Equal(2, injector.Attempts);
    }

    [Fact]
    public void PressStartedWhileDisabled_IsNotCompletedAfterEnabling()
    {
        AppSettings settings = CreateSettings();
        settings.IsEnabled = false;
        RecordingInputInjector injector = new();
        using ClickEngine engine = new(settings, injector, new StubForegroundWindowService(true));

        engine.HandleButtonDown(MouseButtonKind.Left);
        settings.IsEnabled = true;
        engine.HandleButtonUp(MouseButtonKind.Left);

        Assert.False(injector.WaitForCount(1, 100));
    }

    private static AppSettings CreateSettings()
    {
        return new AppSettings
        {
            IsEnabled = true,
            LeftClickEnabled = true,
            RightClickEnabled = false,
            TapMaxDurationMs = 1000,
            ExtraClickDelayMs = 0,
            MinecraftOnly = false
        };
    }

    private static void Tap(ClickEngine engine, MouseButtonKind button)
    {
        engine.HandleButtonDown(button);
        engine.HandleButtonUp(button);
    }

    private sealed class RecordingInputInjector : IInputInjector
    {
        private readonly ConcurrentQueue<MouseButtonKind> _clicks = new();

        public IReadOnlyList<MouseButtonKind> Clicks => _clicks.ToArray();

        public void Click(MouseButtonKind button) => _clicks.Enqueue(button);

        public bool WaitForCount(int count, int timeoutMs = 1000)
        {
            return SpinWait.SpinUntil(() => _clicks.Count >= count, timeoutMs);
        }
    }

    private sealed class FailOnceInputInjector : IInputInjector
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        public ManualResetEventSlim SuccessfulClick { get; } = new();

        public void Click(MouseButtonKind button)
        {
            if (Interlocked.Increment(ref _attempts) == 1)
            {
                throw new InvalidOperationException("Expected test failure.");
            }

            SuccessfulClick.Set();
        }
    }

    private sealed class StubForegroundWindowService(bool isMinecraftFocused) : IForegroundWindowService
    {
        public bool IsMinecraftFocused() => isMinecraftFocused;
    }
}
