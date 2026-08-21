# Privacy Policy

**Sidebar Pc Monitor** — last updated 21 August 2026

## The short version

We collect nothing. There is no account, no telemetry, no analytics, no crash
reporting, and no server of ours for anything to be sent to. The application
reads your hardware's sensors and draws them on your own screen.

Everything below is the detail behind that.

## What stays on your computer

| Data | Where it lives | Leaves your machine? |
|---|---|---|
| Your settings — layout, theme, chosen readings, hotkeys, alert thresholds | `%LocalAppData%\SidebarPcMonitor\settings.json` | **No** |
| Hardware readings — temperatures, clocks, load, memory, drive and network activity, power draw | Memory only, while the app runs | **No** |
| Graph history | Memory only, discarded when the graph closes | **No** |
| Error details, if something goes wrong | Windows Application event log, on your machine | **No** |

Uninstalling removes the settings file along with the application.

## The only two times the app talks to the internet

Both are listed here in full. There are no others — the whole source is public
if you want to check.

### 1. Checking for a new version

**What happens:** on start, the app asks GitHub whether a release newer than the
one you have exists. If there is one it shows a notification. Nothing downloads
or installs until you click it.

**Who receives what:** GitHub receives an ordinary web request, which as with any
web request includes your IP address. We receive nothing — we have no server
involved, and no identifier of any kind is attached to the request. GitHub's
handling of it is covered by
[GitHub's Privacy Statement](https://docs.github.com/site-policy/privacy-policies/github-privacy-statement).

**Default:** on. Turn it off under **Settings → General → Automatic Updates** and
the app will never make this request.

### 2. Showing your external IP address — off by default

**What happens:** the Network panel can display the public IP address your
internet connection presents. There is no way for a program to know that without
asking something outside your network, so the app asks
[api.ipify.org](https://www.ipify.org), a free service that replies with the
address it sees. The request times out after five seconds and the result is
displayed and not stored.

**Who receives what:** ipify receives the request and therefore sees your IP
address, which is what you asked it to tell you. We receive nothing.

**Default: off.** This reading is disabled unless you switch it on yourself, and
no request is made while it is off. If you would rather it never happened, leave
it off.

## The hardware driver

To read CPU temperature, clocks and voltages, the sensor library needs kernel
access, which it gets through [PawnIO](https://pawnio.eu) — an open source
driver bundled with the application and offered on first run.

It runs entirely on your machine, sends nothing anywhere, and the app asks your
permission before installing it. Decline and the app carries on with those
particular readings unavailable.

## Links that open your browser

Menu items such as **GitHub** open a page in your normal browser. At that point
you are simply visiting a website, and that site's privacy policy applies rather
than this one.

## Children

The application is a hardware monitor. It collects nothing from anyone, of any
age.

## Changes to this policy

If the application ever starts doing something that touches your data
differently, this file changes in the same commit as the code, and the
[history of this file](https://github.com/oubahell/Sidebar-Pc-Monitor/commits/main/PRIVACY.md)
is public and permanent. There is no version of this you cannot inspect.

## Contact

Questions or concerns: open an
[issue](https://github.com/oubahell/Sidebar-Pc-Monitor/issues), or a
[private security advisory](https://github.com/oubahell/Sidebar-Pc-Monitor/security/advisories/new)
if it is sensitive.

Maintained by **ObaiDa.A** — https://github.com/oubahell
