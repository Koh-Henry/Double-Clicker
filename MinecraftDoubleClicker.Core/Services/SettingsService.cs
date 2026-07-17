using System;
using System.IO;
using System.Text.Json;
using MinecraftDoubleClicker.Models;

namespace MinecraftDoubleClicker.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _sync = new();

    public SettingsService(string? settingsFilePath = null)
    {
        SettingsFilePath = settingsFilePath ?? GetDefaultSettingsFilePath();
    }

    public string SettingsFilePath { get; }

    public AppSettings Load()
    {
        lock (_sync)
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(SettingsFilePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new AppSettings();
                }

                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                return Normalize(settings);
            }
            catch (IOException)
            {
                return new AppSettings();
            }
            catch (JsonException)
            {
                return new AppSettings();
            }
            catch (UnauthorizedAccessException)
            {
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_sync)
        {
            string directoryPath = Path.GetDirectoryName(SettingsFilePath)
                ?? throw new InvalidOperationException("Unable to resolve settings directory path.");

            Directory.CreateDirectory(directoryPath);

            string json = JsonSerializer.Serialize(Normalize(settings), SerializerOptions);
            string temporaryFilePath = Path.Combine(
                directoryPath,
                $".{Path.GetFileName(SettingsFilePath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                File.WriteAllText(temporaryFilePath, json);
                File.Move(temporaryFilePath, SettingsFilePath, true);
            }
            finally
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
        }
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        AppSettings normalized = settings ?? new AppSettings();

        if (normalized.TapMaxDurationMs < 0)
        {
            normalized.TapMaxDurationMs = 0;
        }

        if (normalized.ExtraClickDelayMs < 0)
        {
            normalized.ExtraClickDelayMs = 0;
        }

        normalized.HotkeyText = string.IsNullOrWhiteSpace(normalized.HotkeyText)
            ? "F8"
            : normalized.HotkeyText.Trim();

        return normalized;
    }

    private static string GetDefaultSettingsFilePath()
    {
        string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppDataPath, "MinecraftDoubleClicker", "settings.json");
    }
}
