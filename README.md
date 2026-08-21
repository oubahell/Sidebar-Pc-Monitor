<div align="center">

<img src="docs/logo.png" width="96" height="96" alt="Sidebar Pc Monitor" />

# Sidebar Pc Monitor

**A Windows desktop sidebar that shows what your PC is actually doing.**

CPU, GPU, RAM, drives, network — and how much power the whole machine is drawing, right now.

[![License](https://img.shields.io/badge/license-GPL--3.0-blue)](LICENSE.md)
[![Platform](https://img.shields.io/badge/platform-Windows%2011%20%7C%2010-0078D4)](#requirements)
[![.NET](https://img.shields.io/badge/.NET%20Framework-4.8.1-512BD4)](#requirements)

</div>

---

<img src="docs/sidebar.png" width="230" align="right" alt="Sidebar Pc Monitor running the Bars layout on the Gamer preset" />

## What it does

Docks a slim panel to the edge of your screen and keeps live hardware readings in
view — no overlay to toggle, no window to alt-tab to.

- **CPU** — temperature, clock, voltage, load, busiest core, power and current draw
- **GPU** — temperature, core and VRAM clocks, load, VRAM usage, power, fan RPM
- **RAM** — physical and virtual memory usage
- **Drives** — space, load and read/write rates
- **Network** — throughput, local and external IP
- **Power** — estimated whole-system wattage and mains current
- Graphs for any reading, alerts on thresholds, and global hotkeys

### Power monitoring

Most monitors stop at per-component numbers. This one adds a **Power** panel that
answers the question you actually care about: *what is this machine costing me right now?*

CPU package watts and GPU board watts are read from real sensors. Everything that
can't report itself — RAM, drives, fans, chipset, VRM losses — is covered by a
configurable estimate, divided by your PSU's efficiency to give draw at the wall,
then by your mains voltage to give amps.

It's labelled **(est.)** for a reason: the two components that dominate and swing
with load are measured, so it tracks your usage honestly, but the absolute figure
carries whatever error is in the overhead estimate. All three values — overhead
watts, PSU efficiency and mains voltage — are yours to tune under
**Settings → Monitors → Power**.

<br clear="right" />

## Make it yours

### Presets — choose how much detail

Enabling every reading at once is a wall of numbers, so pick a starting point:

| Preset | For |
|---|---|
| **Simple** | temperature, load and power at a glance |
| **Gamer** | thermals, headroom, VRAM and fan speed — what explains a frame-rate drop |
| **Advanced** | everything, including voltages, current and per-drive I/O |
| **Custom** | whatever you tick yourself |

### Themes — four colour schemes

**High-Contrast Dark**, **Modern Flat**, **Gaming RGB** and **Windows 11 Fluent**.
Individual colours stay adjustable afterward, with a **Reset Colors** button when
you want the theme's palette back.

### Layouts — and write your own

How each reading is *drawn* is separate from which readings you show. Four ship
built in: **Classic**, **Compact**, **Bars** and **Tiles**.

They're plain XAML files, so you can add your own. Drop one into:

```
%LocalAppData%\SidebarPcMonitor\Layouts\MyStyle.xaml
```

and it appears in **Settings → General → Layout**. No rebuild, no code. Each file
defines a single `DataTemplate` keyed `MetricTemplate`, bound to:

| Binding | What it gives you |
|---|---|
| `Label` / `FullName` | short and long name |
| `Text` | formatted value with units, e.g. `49 C` |
| `Value` | the raw number, for bars and gauges |
| `IsPercent` | true when a bar is meaningful |
| `IsAlert` / `AlertColor` | threshold crossed |

Copy any file in [`SidebarDiagnostics/Layouts/`](SidebarDiagnostics/Layouts) as a
starting point — each is commented as a worked example. A layout that fails to
parse falls back to Classic rather than breaking the app.

## Requirements

- Windows 11, 10, 8.1 or 7
- [.NET Framework 4.8.1](https://dotnet.microsoft.com/download/dotnet-framework/net481)
- Must run **as administrator** — reading hardware sensors needs kernel access

### About the driver

CPU temperature, clocks, voltages and motherboard fan sensors are only readable
through a kernel driver. Since v0.9.5, LibreHardwareMonitor uses
[PawnIO](https://pawnio.eu) for this — without it those readings return `0` while
everything else keeps working.

PawnIO is **bundled** and offered on first run. It's a signed, open-source driver
and the app asks before installing it; decline and the app carries on with the
CPU-sourced readings unavailable.

## Building

Two projects, built separately — the app is a legacy `.csproj` and can't build the
SDK-style sensor library through a project reference:

```powershell
# 1. The sensor library
dotnet build "LibreHardwareMonitor\LibreHardwareMonitorLib\LibreHardwareMonitorLib.csproj" -c Release

# 2. The app
msbuild "SidebarDiagnostics\SidebarPcMonitor.csproj" /p:Configuration=Release
```

See [DEVELOPMENT.md](DEVELOPMENT.md) for architecture notes and the traps worth knowing.

## Credits

Built on [**Sidebar Diagnostics**](https://github.com/ArcadeRenegade/SidebarDiagnostics)
by [**ArcadeRenegade**](https://github.com/ArcadeRenegade) — the original design and
implementation are theirs, and this fork wouldn't exist without it. If you find this
useful, go and star the original too.

Sensor data comes from [**LibreHardwareMonitor**](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor),
with kernel access via [**PawnIO**](https://pawnio.eu) by namazso.

Maintained by **ObaiDa.A**.

## License

**GNU General Public License v3.0** — see [LICENSE.md](LICENSE.md).

Bundled components keep their own licences: LibreHardwareMonitor under MPL-2.0 and
PawnIO under GPL-2.0-or-later. Full attribution and the source offer that PawnIO's
licence requires are in [NOTICE.md](NOTICE.md).
