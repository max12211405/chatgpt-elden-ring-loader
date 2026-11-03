# ChatGPT — Elden Ring Entry Animation v1.0.2

Smooth loading estimates and safer entry/recovery for the Windows companion launcher.

## Changes

- Add a thin gold progress bar with even-height, fixed-width integer percentages.
- Smooth the estimated pace between observed startup stages and adapt it to successful startup times on the current machine. Only confirmed editor readiness permits 100% and entry.
- Replace the unsafe direct-open recovery action with **Check again**. Retry never reveals an unready app or starts the vortex.
- Bound accessibility scans in an owned hidden helper, with timeout, cancellation and cleanup on retry/close. Helpers use the same selected application settings as the host.
- Cancel the entry transition if readiness is lost, including graphics-fallback cancellation, and restore a usable waiting screen.
- Fix immediate renderer preparation after a previous cancelled snapshot.
- Retain the lightweight leaves, mist, grace sparks and original vortex fragments. Dense experimental meteor effects are excluded.
- Add regression coverage for progress pacing, safe recovery, entry cancellation and renderer restart.

## Install or upgrade

Download and fully extract **ChatGPT-Elden-Ring-Loader-v1.0.2-Windows-x64.zip**, then run **Setup.cmd**. Setup selects an installed packaged ChatGPT or Codex app and creates a separate **ChatGPT - Elden Ring** shortcut. The original app and shortcut are independent.

For an upgrade, close the old companion launcher, extract this version to a new writable folder, run Setup, and remove the old companion shortcut when the new one is working. Setup does not silently overwrite an existing shortcut; use `-ShortcutPath` to choose another name if needed. The official app can remain open.

**Optional music is not bundled.** Search for **Elden Ring — Tsukasa Saitoh**, from **Elden Ring (Original Soundtrack)**. Put your own legitimate MP3 at `media/menu-music.mp3` in the extracted launcher folder. Every pass starts at **00:15**; music fades out and stops before handoff. No rebuild is required.

## Validation and limits

Built on Windows x64; native and browser regression tests cover the portable build, readiness/retry policy, owned-helper timeout/cancellation, continuous progress, keyboard entry, graphics fallback, renderer cleanup and generated-audio looping. A fresh physical cold-start/handoff after the new recovery changes and a second-computer test remain outstanding. Automated tests cannot guarantee compatibility with every driver or future official app update.

Requires **Windows 10/11 x64, .NET Framework 4.8, WebView2 Runtime**, and an official packaged ChatGPT or Codex app. Unsigned community build; a SHA-256 checksum is provided with the ZIP.

Original code: MIT. Unofficial fan project, independent of OpenAI, FromSoftware and Bandai Namco. Music is excluded; see the included third-party notices.
