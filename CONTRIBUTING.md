# Contributing

Thanks for taking a look. Bug reports, layout packs and translations are all
welcome.

## Reporting a bug

Open an [issue](https://github.com/oubahell/Sidebar-Pc-Monitor/issues) and say:

- what version you're on (**Settings → About**, or the tray tooltip)
- your Windows version, CPU and GPU
- what you expected and what happened instead

If the app crashed, Windows will have shown an error dialog with a full stack
trace — paste that. It names the exact line, which usually turns an afternoon of
guessing into a five-minute fix.

For anything security-sensitive, please use a
[private security advisory](https://github.com/oubahell/Sidebar-Pc-Monitor/security/advisories/new)
rather than a public issue.

## Building it

Two projects, built separately — the app is a legacy `.csproj` and cannot build
the SDK-style sensor library through a project reference:

```powershell
# 1. The sensor library (only when its source changes)
dotnet build "LibreHardwareMonitor\LibreHardwareMonitorLib\LibreHardwareMonitorLib.csproj" -c Release

# 2. The app
msbuild "SidebarDiagnostics\SidebarPcMonitor.csproj" /p:Configuration=Release
```

Use the MSBuild that ships with Visual Studio 2022 Build Tools. Not
`dotnet msbuild`, and not the one under `C:\Windows\Microsoft.NET` — both fail in
ways that look like your code is broken when it isn't.
[DEVELOPMENT.md](DEVELOPMENT.md) explains why, along with the other traps in this
codebase. It is worth ten minutes before your first change.

## Writing a layout

You do not need to build anything to make a layout. They are plain XAML files
dropped into `%LocalAppData%\SidebarPcMonitor\Layouts\`, and the app picks them
up on restart. Start by copying one of the four in
[`SidebarDiagnostics/Layouts/`](SidebarDiagnostics/Layouts) — each is commented as
a worked example — and see the README for the bindings available.

If you make one you like, a pull request adding it is very welcome.

## Translating

Every user-facing string lives in `SidebarDiagnostics/Properties/Resources.resx`
with one file per language beside it. To add or fix a translation:

1. Edit the `Resources.<lang>.resx` file.
2. If you are adding a new language, add its two-letter code to
   `Culture.Languages` in `Utilities.cs` — a resx file existing is not enough,
   that list is a hardcoded allow-list and the language will not appear without it.

Missing keys fall back to English, so a partial translation is fine and useful.

## Code conventions

- Local variables are prefixed with an underscore (`_sensorList`). This comes
  from the upstream codebase; keep it so the code reads as one piece.
- Comments should explain *why*, not restate *what*. The tricky parts of this
  codebase are tricky for reasons that are not visible in the code.
- New `MetricKey` values need adding to `GetFullName()`, `GetLabel()`, the
  default config, and the resx files.
- Commit messages: explain why, and name the root cause. See the history for tone.

## Pull requests

Verify it compiles and run the app before opening one — a WPF resource problem
will not show up at build time, only when the window it affects opens.

Keep a pull request to one concern. Two small ones are easier to review, and
easier to revert, than one large one.
