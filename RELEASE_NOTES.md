# ChatGPT — Elden Ring Entry Animation v1.0.3

A clearer delayed-start warning and a modest increase in golden meteor trails.

## Changes

- After 30 seconds without a confirmed usable editor, pause and hide the loading estimate instead of leaving a nearly complete percentage on screen.
- Show **There may be a connection issue.** in large italic, warm-gold lettering, with a network/proxy hint that fits the scene.
- Offer only **Check again** on the delayed screen. Retry rechecks readiness; it never opens an unready app or starts the entry transition. Confirmed readiness restores **Continue journey** and **Press Enter**.
- Increase the existing golden meteor fragments from 44 to 84 and slightly lengthen their trails. Reuse the same renderer, keeping the fixed camera, leaves, mist and grace sparks.
- Refresh the startup and transition previews, add a connection-warning preview, and cover delayed readiness and recovery in the browser suite.

## Install or upgrade

Download and fully extract **ChatGPT-Elden-Ring-Loader-v1.0.3-Windows-x64.zip**, then run **Setup.cmd**. Setup selects an installed packaged ChatGPT or Codex app and creates a separate **ChatGPT - Elden Ring** shortcut.

For an upgrade, close the old companion launcher, extract this version to a new writable folder, run Setup, and remove the old companion shortcut when the new one is working. Setup does not silently overwrite an existing shortcut; use `-ShortcutPath` to choose another name if needed. The official app can remain open.

**Optional music is not bundled.** Search for **Elden Ring — Tsukasa Saitoh**, from **Elden Ring (Original Soundtrack)**. Put your own legitimate MP3 at `media/menu-music.mp3` in the extracted launcher folder. Every pass starts at **00:15**; music fades out and stops before handoff. No rebuild is required.

## Validation and limits

The warning indicates delayed editor detection and suggests a possible connection problem; it is not a network diagnosis. The companion cannot repair the official app's network or sign-in failures.

Built on Windows x64. Native and browser tests cover the portable build, readiness/retry policy, owned-helper timeout/cancellation, delayed-screen recovery, continuous progress, keyboard entry, graphics fallback, renderer cleanup and generated-audio looping. A fresh physical cold-start/handoff and a second-computer test remain outstanding. Automated checks do not certify every driver or future official app update.

Requires **Windows 10/11 x64, .NET Framework 4.8, WebView2 Runtime**, and an official packaged ChatGPT or Codex app. Unsigned community build; a SHA-256 checksum is provided with the ZIP.

Original code: MIT. Unofficial fan project, independent of OpenAI, FromSoftware and Bandai Namco. Music is excluded; see the included third-party notices.
