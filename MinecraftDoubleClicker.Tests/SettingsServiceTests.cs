using MinecraftDoubleClicker.Models;
using MinecraftDoubleClicker.Services;

namespace MinecraftDoubleClicker.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public void Load_MissingFileReturnsDefaults()
    {
        using TemporaryDirectory directory = new();
        SettingsService service = new(Path.Combine(directory.Path, "settings.json"));

        AppSettings settings = service.Load();

        Assert.True(settings.IsEnabled);
        Assert.True(settings.LeftClickEnabled);
        Assert.False(settings.RightClickEnabled);
        Assert.Equal("F8", settings.HotkeyText);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllSettings()
    {
        using TemporaryDirectory directory = new();
        string path = Path.Combine(directory.Path, "nested", "settings.json");
        SettingsService service = new(path);
        AppSettings expected = new()
        {
            IsEnabled = false,
            LeftClickEnabled = false,
            RightClickEnabled = true,
            TapMaxDurationMs = 250,
            ExtraClickDelayMs = 33,
            MinecraftOnly = false,
            HotkeyText = "Ctrl+F9"
        };

        service.Save(expected);
        AppSettings actual = service.Load();

        Assert.False(actual.IsEnabled);
        Assert.False(actual.LeftClickEnabled);
        Assert.True(actual.RightClickEnabled);
        Assert.Equal(250, actual.TapMaxDurationMs);
        Assert.Equal(33, actual.ExtraClickDelayMs);
        Assert.False(actual.MinecraftOnly);
        Assert.Equal("Ctrl+F9", actual.HotkeyText);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
    }

    [Fact]
    public void Load_CorruptJsonReturnsDefaults()
    {
        using TemporaryDirectory directory = new();
        string path = Path.Combine(directory.Path, "settings.json");
        File.WriteAllText(path, "{not valid json");
        SettingsService service = new(path);

        AppSettings settings = service.Load();

        Assert.True(settings.LeftClickEnabled);
        Assert.False(settings.RightClickEnabled);
        Assert.Equal("F8", settings.HotkeyText);
    }

    [Fact]
    public void Save_NormalizesInvalidDurationsAndHotkey()
    {
        using TemporaryDirectory directory = new();
        SettingsService service = new(Path.Combine(directory.Path, "settings.json"));
        AppSettings settings = new()
        {
            TapMaxDurationMs = -10,
            ExtraClickDelayMs = -20,
            HotkeyText = "  "
        };

        service.Save(settings);
        AppSettings loaded = service.Load();

        Assert.Equal(0, loaded.TapMaxDurationMs);
        Assert.Equal(0, loaded.ExtraClickDelayMs);
        Assert.Equal("F8", loaded.HotkeyText);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MinecraftDoubleClicker.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
