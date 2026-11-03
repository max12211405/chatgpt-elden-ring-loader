# Compatibility and troubleshooting

## Supported scope

- Windows 10/11, x64.
- .NET Framework 4.8.
- Microsoft Edge WebView2 Runtime, with a functional graphics driver.
- An official **packaged** Windows ChatGPT or Codex app registered in the Start menu.
- A writable extracted launcher folder. Run from the extracted folder, not inside a ZIP.

The launcher has been exercised on a Windows 11 system with 125% display scaling. Layout tests also cover other frame sizes and negative monitor origins. This is not a claim of physical testing on every monitor arrangement or machine.

macOS, Linux, ARM64, browser-only ChatGPT, and arbitrary unpackaged EXE installations are not supported by this release. Source changes would be needed for those targets.

## Setup and app identity

`Setup.ps1` discovers matching entries through `Get-StartApps` and reads the selected package manifest to determine its executable name. If manifest access is restricted, it checks the package identity of running processes. The exact initial package also has a verified executable-name fallback. Unknown installations can be configured explicitly. It writes local `launcher.json`; the source tree contains no user's generated configuration.

If automatic selection cannot determine the executable, use Windows PowerShell to inspect installed entries and then supply both fields explicitly:

```powershell
Get-StartApps | Where-Object { $_.Name -match 'Codex|ChatGPT' }
.\Setup.ps1 -AppId 'Your.PackageFamily!App' -ProcessName 'YourExecutableName'
```

Use the real application ID from your installation and the executable name without `.exe`. Do not paste the example ID literally. `windowTitle` is optional; an empty value accepts a suitably sized visible window belonging to that exact package identity.

Moving the extracted folder changes the shortcut target. Run setup with a new `-ShortcutPath`, or remove the old companion shortcut before recreating it. Setup never silently overwrites an existing shortcut.

## Runtime checks

- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

The SDK DLLs ship in the release; the WebView2 **Runtime** is a separate Microsoft component. If initialization fails, the launcher tries to open the actual app directly. No runtime installer is silently downloaded or executed.

## The scene does not appear

The configured app's verified main process may still be running in the background. This deliberately takes the fast path. Close it using the official app's own Exit/Quit command, or use `--demo` to inspect the scene without ending your conversation. Do not terminate unrelated processes just to force the animation.

## Continue stays unavailable

Readiness uses Windows UI Automation to detect a usable editor. An official app update, login screen, accessibility change, or modal can prevent that detection. After 30 seconds, **Check again** restarts the check; it cannot bypass readiness or reveal an unready app. Each scan runs in an owned, hidden helper that times out after five seconds. A retry or closing the loader cancels that helper.

The thin loading bar shows an estimated integer percentage, not an internal app loading API. Its pace adapts to recent successful launches on the current machine. Stage ceilings prevent a slow or stalled startup from reporting 100% prematurely. Local timing history is stored in the scene's browser storage; demo mode does not train it. Resetting WebView data clears the estimate's history.

If the official app is stuck at login or a modal, close the loader with its native title-bar X and finish that step in the official app. Do not expect repeated clicks on Check again to dismiss an app dialog.

## Continue appears to do nothing

The button immediately changes to **Entering...**. Snapshot preparation and the transition take a few seconds. Foreground activation is verified asynchronously; transient delays are retried inside the same entry action. If you deliberately switch to another app, the guarded handoff may cancel and restore the scene.

## Black screen or graphics failure

Overlay/injection tools can conflict with embedded graphics. A native test encountered an access violation in an RTSS overlay module. Fatal browser/main-renderer failures now open the actual app directly and dispose the animation. That fatal-event recovery was added and compiled; it has not been exhaustively fault-tested against every overlay.

The scene also has a canvas fallback when WebGL is unavailable and a simpler fade for reduced-motion settings. Keep graphics drivers and WebView2 Runtime maintained through their normal vendor channels.

Version 1.0.2 retains the lightweight scene and original vortex fragments. Dense experimental meteor effects are not included. Automated build/regression checks cover the new recovery logic, but a fresh physical cold-start/handoff and a second-computer test of this version remain outstanding.

## Privacy and issue reports

The companion does not send chats, read message contents, or call an AI API. Readiness queries editor control properties and bounds. It activates the installed app using its Windows application ID. The scene is local; the official app keeps its own network behavior.

Local diagnostics are stored in `launcher.log`; WebView2 stores its local browser cache under `WebViewData`. Neither is included in source or release archives. Review and redact machine paths, identifiers, and private information before attaching diagnostics to a public issue. Do not attach cache folders or crash dumps.
