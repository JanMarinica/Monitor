# Screen Monitor

A Windows GUI application that watches a selected area of the screen for pixel changes and automatically clicks a chosen spot when a change is detected.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & Run

```
cd ScreenMonitor
dotnet run
```

Or publish a self-contained exe:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Usage

1. **Pick Monitor Region** — click the button, then drag a rectangle over the area you want to watch.
2. **Pick Click Spot** — click the button, then click the exact pixel you want auto-clicked.
3. Adjust **Poll interval**, **Change threshold**, and **Cooldown** with the sliders.
4. Click **▶ Start Monitoring**.

The live preview panel shows a thumbnail of the monitored region and a diff bar at the bottom (green → orange → red as change increases).

Settings are saved to `config.json` next to the executable.
