using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenMonitor;

public sealed class Config
{
    public MonitorRegion MonitorRegion { get; set; } = new();
    public ClickTarget ClickTarget { get; set; } = new();

    // Milliseconds between screen captures
    public int PollIntervalMs { get; set; } = 500;

    // Percentage of pixels that must differ to trigger a click (0–100)
    public double ChangeThresholdPercent { get; set; } = 2.0;

    // Minimum seconds to wait after a click before clicking again
    public double CooldownSeconds { get; set; } = 2.0;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
    private const string FileName = "config.json";

    public static Config Load()
    {
        if (!File.Exists(FileName))
            return new Config();

        try
        {
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(FileName), JsonOpts) ?? new Config();
        }
        catch
        {
            return new Config();
        }
    }

    public void Save()
    {
        File.WriteAllText(FileName, JsonSerializer.Serialize(this, JsonOpts));
    }
}

public sealed class MonitorRegion
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;

    [JsonIgnore]
    public bool IsSet => Width > 0 && Height > 0;

    [JsonIgnore]
    public Rectangle ToRectangle() => new(X, Y, Width, Height);
}

public sealed class ClickTarget
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;

    [JsonIgnore]
    public bool IsSet => X != 0 || Y != 0;
}
