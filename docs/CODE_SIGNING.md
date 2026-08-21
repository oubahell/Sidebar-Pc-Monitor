# Code signing policy

This document describes how release binaries for **Sidebar Pc Monitor** are
built, who may authorise a signature, and what a signature on one of our files
is meant to tell you.

## Who can approve a signature

| Role | Person | Rights |
|---|---|---|
| Maintainer / Approver | **ObaiDa.A** ([@oubahell](https://github.com/oubahell)) | Sole owner of the repository. Only person able to approve a signing request. |

There is currently one maintainer. Should that change, this table is updated in
the same commit that grants the access.

Multi-factor authentication is required on the GitHub account and on the signing
platform account. No signing request is approved from an account without it.

## What gets signed

Only artifacts produced by the release workflow from a tagged commit in this
repository:

| Artifact | What it is |
|---|---|
| `SidebarPcMonitor.exe` | the application |
| `uninstall.exe` | a copy of the above that hands over to the updater |
| `SidebarPcMonitor-win-Setup.exe` | per-user installer |
| `SidebarPcMonitor-win.msi` | machine-wide installer |

Nothing is signed by hand, from a developer machine, or from an untagged commit.

## How a release is built

Every release is produced by
[`.github/workflows/release.yml`](../.github/workflows/release.yml), triggered by
pushing a version tag, and runs entirely on a GitHub-hosted runner from the
public source in this repository. No step depends on a local machine, and no
binary is copied in from anywhere a reader cannot inspect.

The workflow:

1. Checks out the tagged commit.
2. Refuses to continue unless the tag and `AssemblyInfo.cs` state the same
   version — so an installed copy can never report a version other than the
   release it came from.
3. Builds `LibreHardwareMonitorLib` from the source vendored in this repository.
4. Restores NuGet packages and builds the application with MSBuild.
5. Stages the output, dropping debug symbols.
6. Packages with [Velopack](https://velopack.io) into an installer, an MSI and a
   portable archive.
7. Publishes a **draft** release. A human reviews and publishes it.

Anyone can read the log of any release build in the
[Actions tab](https://github.com/oubahell/Sidebar-Pc-Monitor/actions).

## Third-party binaries in the package

One binary in this repository is not built by us:

**`SidebarDiagnostics/Resources/PawnIO_setup.exe`** — the installer for
[PawnIO](https://pawnio.eu), the kernel driver LibreHardwareMonitor uses to read
CPU temperature, clocks and voltages. It is embedded in the application and
offered to the user on first run; the app asks before installing it, and
declining leaves the app working with those readings unavailable.

It is redistributed unmodified and **already carries a valid Authenticode
signature from its author**:

```
CN = namazso.eu, O = namazso, L = Debrecen, C = HU
```

PawnIO is licensed GPL-2.0-or-later with a device-IOCTL exception. Attribution
and the written offer of source that its licence requires are in
[NOTICE.md](../NOTICE.md).

## What our signature means

That the file was built by the workflow above, from the public source at the
tagged commit, and has not been altered since.

It is **not** a warranty. The software is provided under the GNU General Public
License v3.0, without warranty of any kind — see [LICENSE.md](../LICENSE.md).

## Reporting a problem

If you find a signed binary that does not match the source it claims to come
from, or anything else security-relevant, please open a
[private security advisory](https://github.com/oubahell/Sidebar-Pc-Monitor/security/advisories/new)
rather than a public issue.
