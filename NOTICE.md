# Third-Party Attribution

## Sidebar Diagnostics

This project, **Sidebar Pc Monitor**, is a fork of [Sidebar Diagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics), created by **ArcadeRenegade** (https://github.com/ArcadeRenegade).

The original project is licensed under the GNU General Public License v3.0 (see [LICENSE.md](LICENSE.md)). This fork retains that license for all code derived from the original work, per the terms of the GPL-3.0.

## LibreHardwareMonitor

Hardware sensor data is read using [LibreHardwareMonitorLib](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), maintained by the LibreHardwareMonitor contributors. It is licensed under the Mozilla Public License 2.0 (see [LibreHardwareMonitor/LICENSE](LibreHardwareMonitor/LICENSE) and [LibreHardwareMonitor/THIRD-PARTY-NOTICES.txt](LibreHardwareMonitor/THIRD-PARTY-NOTICES.txt)).

This fork vendors LibreHardwareMonitorLib source directly (as the original project did via git submodule) so it can be kept current with upstream releases.

## PawnIO

This application **redistributes the PawnIO installer** (`SidebarDiagnostics/Resources/PawnIO_setup.exe`), created by **namazso** (admin@namazso.eu). Source: https://github.com/namazso/PawnIO — official site: https://pawnio.eu

PawnIO is a scriptable kernel driver that provides the low-level hardware access LibreHardwareMonitor needs. From version 0.9.5, LibreHardwareMonitor removed its previous self-installing WinRing0 driver and routes all MSR/SMN/LPC reads through PawnIO instead; without it, CPU temperature, core clocks, package power and Super I/O (board temperature and fan) readings all return zero.

PawnIO is licensed under the **GNU General Public License, version 2 or (at your option) any later version**. Because it is offered as "version 2 or later", it is redistributed here under GPL-3.0 alongside the rest of this application.

Its licence additionally grants this exception, which is what permits the combination used here:

> In addition, as a special exception, the copyright holders of PawnIO give you permission to combine PawnIO program with free software programs or libraries that are released under the GNU LGPL and with independent modules that communicate with PawnIO solely through the device IO control interface. You may copy and distribute such a system following the terms of the GNU GPL for PawnIO and the licenses of the other code concerned, provided that you include the source code of that other code when and as the GNU GPL requires distribution of source code.

LibreHardwareMonitor communicates with PawnIO solely through that device IO control interface (`CreateFile(\\.\PawnIO)` plus `DeviceIoControl`), so this application falls within the exception.

**Written offer of source code:** the complete corresponding source for the redistributed PawnIO binary is available from https://github.com/namazso/PawnIO. The bundled installer is the unmodified official release, downloaded from https://github.com/namazso/PawnIO.Setup/releases.

The installer is only run after the user explicitly agrees to the prompt shown on first launch; declining is remembered and the application continues without it.
