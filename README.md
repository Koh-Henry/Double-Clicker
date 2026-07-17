# Double Clicker

A desktop utility that adds one extra click after a short left- or right-mouse-button tap. It supports Windows and macOS, can be limited to Minecraft, and can be toggled with a global hotkey.

## Use

1. Choose whether left clicks, right clicks, or both should be doubled.
2. Set the maximum tap duration and delay before the added click.
3. Leave **Only active when Minecraft is focused** checked for normal use.
4. Select **Save**. Settings are kept in the current user's local application-data directory.

Injected clicks are marked and ignored by the listener, so they do not recursively produce more clicks.

## Build a Windows `.exe`

From PowerShell:

```powershell
.\scripts\publish-windows.ps1
```

The self-contained, single-file application is written to `artifacts\win-x64\MinecraftDoubleClicker.exe`. A compatible .NET runtime does not need to be installed on the destination computer.

## Build a macOS `.app`

Run on the destination Mac (Apple Silicon by default, or pass `x64` for an Intel build):

```bash
./scripts/build-macos.sh
./scripts/build-macos.sh x64
```

The app bundle is written beneath `artifacts/osx-arm64` or `artifacts/osx-x64`. On first use, macOS must grant the app both **Input Monitoring** and **Accessibility** access under **System Settings > Privacy & Security**. Restart the app after granting access.

For distribution to other users, build on macOS and sign with a Developer ID Application certificate:

```bash
SIGNING_IDENTITY="Developer ID Application: Your Name (TEAMID)" ./scripts/build-macos.sh
```

Then submit the signed `.app` with Apple's `notarytool` and staple the result. Signing and notarization require an Apple Developer account and cannot be completed from this Windows checkout without those credentials.

## Architecture

`MinecraftDoubleClicker.Core` contains platform-neutral tap timing, scheduling, hotkey parsing, and settings persistence. The desktop app supplies native input injection, global mouse monitoring, foreground-app detection, and hotkeys through small interfaces. The shared UI uses Avalonia.

Windows uses Win32 hooks, `SendInput`, and `RegisterHotKey`. macOS uses Quartz event taps and event posting; foreground-app detection uses `NSWorkspace`. Linux is intentionally rejected until equivalent native services are implemented.

## Tests

Run the cross-platform unit suite with:

```powershell
dotnet test --solution MinecraftDoubleClicker.sln
```

The suite exercises left/right tap behavior, timing, focus restrictions, pending-click cancellation, scheduler ordering, failure recovery, hotkey parsing, and settings persistence. Native end-to-end input injection still requires manual verification on each operating system because CI should not generate real global mouse clicks.
