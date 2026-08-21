# Sidebar Pc Monitor

A Windows desktop sidebar (C# / WPF, .NET Framework 4.8.1) that displays live hardware sensor
readings. Private repo: `oubahell/Sidebar-Pc-Monitor`.

This is a **fork of [ArcadeRenegade/SidebarDiagnostics](https://github.com/ArcadeRenegade/SidebarDiagnostics)**
(GPL-3.0), rebranded and modernised. Attribution to the original author is deliberate and required
by the licence — keep it in `README.md` and `NOTICE.md`.

The maintainer is **ObaiDa.A**. He is not a native English speaker and writes terse, sometimes
all-caps requests; that is not annoyance, just brevity. He tests changes himself on real hardware and
will tell you plainly when something looks wrong.

---

## Build and run — read this before touching anything

### The project is two builds, not one

`LibreHardwareMonitorLib` is an SDK-style multi-target project; the app is a legacy
(non-SDK) `.csproj`. The old MSBuild **cannot resolve `Microsoft.NET.Sdk`**, so it cannot build the
library via `ProjectReference`. They are built separately and joined by a plain DLL reference:

```powershell
# 1. Library (only when LHM source changes) — needs a global.json pinning SDK 9
dotnet build "LibreHardwareMonitor\LibreHardwareMonitorLib\LibreHardwareMonitorLib.csproj" -c Release

# 2. The app
& "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe" `
  "SidebarDiagnostics\SidebarPcMonitor.csproj" /p:Configuration=Release /p:Platform="AnyCPU"
```

The app references `LibreHardwareMonitor\bin\Release\AnyCPU\net472\LibreHardwareMonitorLib.dll`
plus its transitive dependencies, all listed explicitly in the `.csproj`.

### The running app locks the .exe and you usually cannot kill it

`app.manifest` sets `requireAdministrator`, so the running app is elevated. A non-elevated agent
shell **cannot** `Stop-Process` it, and the build then fails with `MSB3027 … file is locked`.

**Ask the user to close the app.** Do not spin on retries.

To type-check without deploying, build to a scratch folder:

```powershell
& $msbuild "...\SidebarPcMonitor.csproj" /p:Configuration=Release `
   /p:OutputPath=$tmp /p:BaseIntermediateOutputPath="$tmp\obj\\"
```

⚠️ **A scratch build is not a deploy.** This was gotten wrong once: a fix was verified into a temp
folder, reported as done, and the user hit the identical crash because `bin\Release` still held the
old binary. Always deploy to the real output before saying it is fixed.

### Verifying visually

The user's layout: virtual screen `4480x1440`, origin `(-2560,-182)`; sidebar docks right at
`1740,0,1920,1032`. Screenshot the virtual screen and crop. Window rects come from `GetWindowRect`
in the same coordinate space as `SetCursorPos`, so enumerate windows to get exact coordinates rather
than guessing — guessed crops wasted several rounds.

Simulated clicks to drive the Settings UI are **unreliable** (first click often only focuses the
window; tabs frequently do not switch). Prefer temporarily setting `SelectedIndex` on the TabControl,
and always strip such scaffolding before committing.

---

## Layout of the code

Folder is still named `SidebarDiagnostics/` and the root namespace is still `SidebarDiagnostics` —
only the assembly, `.csproj` and `.sln` were renamed to `SidebarPcMonitor`. Renaming the namespace
was judged not worth the churn.

| File | Role |
|---|---|
| `Monitoring.cs` | ~3.5k lines. Sensor discovery, all metric/monitor/panel types, enums, presets. The heart. |
| `Sidebar.xaml` | The bar itself. Metric rows bind to `{DynamicResource MetricTemplate}`. |
| `App.xaml` | Global styles and control templates. Very large. |
| `FlatStyle.xaml` | Window chrome and the button hierarchy. |
| `Themes/*.xaml` | 4 colour themes (brush tokens only). |
| `Layouts/*.xaml` | 4 metric layouts (`DataTemplate` only). |
| `LayoutManager.cs` | Loads built-in + user layouts. |
| `PawnIOSetup.cs` | Installs the bundled kernel driver. |
| `Settings.cs` | Persisted settings singleton (`Framework.Settings.Instance`). |
| `SettingsModel.cs` | View model for the Settings dialog. |

---

## Hard-won knowledge — the traps

### 1. Never match sensors by index. Match by name.

This caused **three separate bugs**. LibreHardwareMonitor changed its indexing between versions, and
the original code hardcoded indices:

- **CPU cores:** LHM added an aggregate `CPU Core Max` at index 1, shifting every real core up one.
  The old `for i = 1..max` loop produced one bogus row *and* mislabelled all the rest (17 rows on a
  16-thread CPU).
- **RAM:** memory sensors are numbered *globally across devices*. `Total Memory` owns Load#0 /
  Data#0,#1; `Virtual Memory` gets Load#1 / Data#2,#3. Index checks matched nothing on Virtual
  Memory, which then rendered as an **empty block** — a mysterious gap in the sidebar.
- **The fix's own bug:** a loose fallback to "first Data sensor" then picked up each DIMM's
  *Capacity* and displayed it as "Used: 8 GB". Fallbacks must be constrained (`Name.StartsWith("Memory")`).

There is also a defensive guard in `OHMMonitor.GetInstances` that drops monitors with zero metrics,
so a mismatch can never again show as blank space.

### 2. CPU temperature/clock/power require PawnIO — this is not an app bug

LHM **0.9.5 removed WinRing0** and routes all MSR/SMN/LPC access through
[PawnIO](https://pawnio.eu). Without it the sensors still enumerate but every CPU-sourced reading is
`0` or `null`, while OS-sourced readings (per-core load) and driver-sourced ones (GPU temps) work
fine. **That asymmetry is the diagnostic signature.**

Confirmed at source level: `Amd17Cpu.cs:190` reads temperature only via `_pawnModule.ReadSmn(...)`.
The only `Ring0` references left are commented out — there is no fallback.

The installer is **bundled** (`Resources/PawnIO_setup.exe`, embedded) and installed on first run
after a prompt. Silent switches are `-install -silent` (note: dashes, not `/S`). Detection is a
registry key, `HKLM\...\Uninstall\PawnIO` — a running driver alone is not enough.

**Licensing (checked, not assumed):** PawnIO is GPL-2.0-**or-later** with an explicit exception
permitting combination with modules that talk to it *solely over the device IO control interface* —
which is exactly how LHM uses it. "or later" lets it be taken as GPL-3.0 to match this app. See
`NOTICE.md` for the notice and source offer, which are **required** — do not remove them.

### 3. WPF ambient/implicit styles silently shadow each other

Repeatedly cost time while theming:

- A local `Style.Resources` entry (e.g. `SettingGrid` declaring a bare `ComboBox` style) **completely
  replaces** a global implicit style unless it uses `BasedOn`. Several controls stayed white for this
  reason alone.
- **Property setters are not enough** for `TextBox`, `ComboBox`, `CheckBox`, `Slider`, `ContextMenu`,
  `DataGrid`. The default Aero2 templates ignore `Background`/`Foreground`; they need full
  `ControlTemplate` replacements.
- A `DockPanel`-scoped `TextBox` style (width 50, docked right) leaked into an unrelated popup and
  clipped it. Give popup content explicit keyed styles.
- Third-party controls can be worse: Xceed's `ColorPicker` paints from a hardcoded light palette
  inside its own template and ignored implicit styles, `ButtonStyle` and `SystemColors` overrides
  alike. Overriding its `ButtonStyle` also broke its drop-down (it drives open state through
  `PART_ColorPickerToggleButton`) and left the palette stuck open on load. It was **replaced** with
  `Controls/ColorPickerBox.xaml`, which keeps the same `"#RRGGBB"` two-way binding contract.

Per-theme `SystemColors` brush overrides live in each `Themes/*.xaml` to catch other third-party
popups.

### 4. Settings resets must not leave state only valid after a restart

`Settings.Reset()` originally set `MonitorConfig = null`, relying on `CheckConfig` at startup to
regenerate it — but the Settings dialog rebuilds its view model **immediately**, read the null and
threw. It now calls `MonitorConfig.CheckConfig(null)` in place.

`Reset()` assigns through **public setters on the existing instance** rather than replacing the
singleton, because XAML binds via `x:Static frame:Settings.Instance` — swapping the object would
leave every binding pointing at the discarded one.

It deliberately preserves `ChangeLog`, `InitialSetup` and `SkipDriverPrompt` (bookkeeping, not
preferences).

---

## Systems added in this fork

- **Themes** (`Themes/*.xaml`) — 4 presets. Brush tokens only: `Chrome{Background,Foreground,Border,
  Accent,AccentHover,Muted,Surface,SurfaceHover,Danger}Brush` + `ChromeShadowColor`. Swapped at
  runtime by `App.ApplyTheme`.
- **Layouts** (`Layouts/*.xaml`) — Classic / Compact / Bars / Tiles. Each supplies exactly one
  `DataTemplate` keyed `MetricTemplate`. **Users can add their own** by dropping a `.xaml` into
  `%LocalAppData%\SidebarPcMonitor\Layouts\`; loaded via `XamlReader.Load`, invalid files fall back
  to Classic. Use `DynamicResource` inside layouts so lookups survive merge order.
- **Metric presets** — Simple / Gamer / Advanced / Custom (`MetricPresets` in `Monitoring.cs`).
  Custom is *derived*, never chosen: editing any metric re-runs `Detect()`. `Apply()` also switches a
  **panel** off when a preset gives it nothing to show (otherwise Drives sat there empty).
  **Simple is the shipped default** — enabling everything is a wall of numbers.
- **Power panel** — the only panel that aggregates across hardware types. CPU package W and GPU
  board W are **measured**; RAM/drives/fans/chipset are a configurable estimate (default 60 W),
  divided by PSU efficiency, then by mains voltage for amps. Labelled **"(est.)"** on purpose, and
  shows `"No Value"` if no real sensor is readable rather than displaying the overhead constant as
  though it were a measurement.
- **New metrics** — `CPUPower`, `CPUCurrent` (SMU **TDC**, not EDC — EDC reports the electrical
  *limit*, a flat 90 A), `GPUPower`, `GPUFanRPM` (`SensorType.Fan` = real RPM; the pre-existing
  `GPUFan` is `SensorType.Control` = fan-curve %), `CPUCoreMax`.
- **UI rework** — button hierarchy (`WindowButton` primary / `SecondaryButton` / `NeutralButton`
  ghost), flat Win11-style caption buttons, themed checkboxes and sliders, Reset Colors, Reset to
  Defaults.

---

## Releasing — follow this every time

The maintainer asked for this explicitly: **every bug fix of substance or new feature gets a version
bump and goes to GitHub on its own branch.** Do not accumulate a pile of unversioned work on `main`.

Bump the **patch** number each time — `4.0.1`, `4.0.2`, `4.0.3` … Reserve a minor bump (`4.1.0`) for
something genuinely large, and only with the maintainer's agreement.

Each release:

1. **Version** — `Properties/AssemblyInfo.cs`, both `AssemblyVersion` and `AssemblyFileVersion`
   (`4.0.1.0`). This also drives the tray tooltip and the changelog window.
2. **Changelog** — prepend an entry to `SidebarDiagnostics/ChangeLog.json`. Write it for the *user*:
   what changed and what it means for them, not the implementation. The app shows this dialog on
   first run of a new version.
3. **Branch** — `git checkout -b v4.0.1`. One branch per version.
4. **Commit** — explain *why* and name the root cause (see history for tone).
5. **Push** — `git push -u origin v4.0.1`. Leave merging to `main` to the maintainer.

Verify it compiles before committing. If the app is running and holding the `.exe`, a scratch-folder
build type-checks — but **never** report a fix as done off a scratch build (see the warning above).

## Conventions

- Local variables are prefixed `_` (`_sensorList`) — matches the upstream codebase; keep it.
- Every user-facing string goes in `Resources.resx` **and** `Resources.ar.resx`, plus a manual
  property in `Resources.Designer.cs` (the designer is not auto-regenerated by this build).
- Arabic is fully supported and the user checks it. `Culture.Languages` in `Utilities.cs` is a
  hardcoded allow-list — a translation file existing is not enough; `ar` and `tr` had to be added
  there before they appeared in the dropdown. The dropdown lists *specific* cultures (`ar-SA`), not
  neutral (`ar`).
- Commit messages: explain **why**, and name the root cause. See existing history for tone.
- New `MetricKey` values must be added to `GetFullName()`, `GetLabel()`, the default config, and both
  resx files. Saved configs merge against defaults, so new keys reach existing users automatically.

## Known-open items

- Total system wattage is an estimate; never validated against a wall meter. The user intends to.
- Top/bottom docking is **half-built**: `DockEdge` already declares `Top`/`Bottom`, but only
  `SetWidth` exists (needs `SetHeight`), the dropdown offers only Left/Right, and panels would need
  to flow horizontally.
- Acrylic/blur backdrop discussed, not started.
- The user's next stated interest is more work on presentation style.
