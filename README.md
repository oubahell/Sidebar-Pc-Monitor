<h1><img src="SidebarDiagnostics/Sidebar.ico" width="48" height="48" /> Sidebar Pc Monitor</h1>

A Windows desktop sidebar that displays live hardware diagnostic information (CPU, RAM, GPU, network, drives, and more).

Maintained by **ObaiDa.A**.

### About this project

**Sidebar Pc Monitor** is a fork of [**Sidebar Diagnostics**](https://github.com/ArcadeRenegade/SidebarDiagnostics) by [**ArcadeRenegade**](https://github.com/ArcadeRenegade). All credit for the original design and implementation goes to them — huge thanks for building and open-sourcing it.

This fork exists to keep the project moving forward with:

* An updated, current [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) core (the original used an older pinned version).
* A new visual identity: new name, new icon, and a set of selectable themes.
* New features and quality-of-life improvements.
* Ongoing maintenance, including Windows 11 support.

If you find this useful, please also check out the original repository and consider supporting **ArcadeRenegade**.

> This fork's own donate link is disabled for now — it'll be added once one exists.

### Original features

* Monitors CPU, RAM, GPU, network, and logical drives.
* Create graphs for all metrics.
* Allows for lots of customization.
* Allows alerts for various values.
* Allows binding hotkeys.
* Supports monitors of all DPI types.
* Has a clock at the top.

### Info

Written in C# / .NET WPF, currently targeting .NET Framework 4.8.1.

You will need to run it as administrator (required by the hardware monitoring library to read sensor data).

Hardware data is provided by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — please thank that project's contributors too!

### Supported OS

* Windows 11
* Windows 10
* Windows 8 / 8.1
* Windows 7

### License

This project is licensed under the **GNU General Public License v3.0** (see [LICENSE.md](LICENSE.md)), the same license as the upstream project. The bundled LibreHardwareMonitor library is licensed under **MPL-2.0** (see [LibreHardwareMonitor/LICENSE](LibreHardwareMonitor/LICENSE)).

See [NOTICE.md](NOTICE.md) for full third-party attribution.
