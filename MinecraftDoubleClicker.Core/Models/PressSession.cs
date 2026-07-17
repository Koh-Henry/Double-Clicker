namespace MinecraftDoubleClicker.Models;

public sealed class PressSession
{
    public bool IsTracking { get; set; }

    public long DownTimeStamp { get; set; }
}
