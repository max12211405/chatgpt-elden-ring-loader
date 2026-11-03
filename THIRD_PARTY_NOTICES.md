# Third-party notices

## Project source and illustrations

The original launcher source and documentation are MIT licensed. `assets/background.png` and `assets/grace-patch.png` are AI-assisted fan illustrations created for this scene, rather than redistributed in-game screenshots or extracted game textures. The project offers its own contributions to these illustrations under the same MIT terms to the extent those contributions are protectable and licensable. This does not grant rights to underlying third-party characters, settings, trademarks, or other intellectual property.

The theme is inspired by ELDEN RING. ELDEN RING and its associated intellectual property belong to FromSoftware, Bandai Namco, and their respective rights holders. Codex and ChatGPT names and marks belong to OpenAI. No endorsement or affiliation is implied.

## html2canvas

- Version: 1.4.1
- Author: Niklas von Hertzen and contributors
- License: MIT
- Source: https://github.com/niklasvh/html2canvas
- License text: `vendor/html2canvas-LICENSE.txt` in source, `html2canvas-LICENSE.txt` in the release.

## Microsoft WebView2 SDK

- Version: 1.0.4314.35
- Source: https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4314.35
- Restored from the official NuGet endpoint and SHA-256 verified by the build.
- Redistributed SDK binaries retain the included `WebView2-LICENSE.txt` notice.
- The Microsoft Edge WebView2 Runtime is not bundled and is governed by Microsoft's own terms.

## Fonts and app icons

No Windows font files or extracted official app icons are included in source or release archives. Typography uses system-installed fonts. At runtime the companion may read the icon from the locally installed target app for its window; that local extraction does not add the icon to published packages.

## Music

No game soundtrack recording is included. The optional recommended track is **Elden Ring** by **Tsukasa Saitoh**, from **Elden Ring (Original Soundtrack)**. Users supply a legitimate local audio file themselves. Music rights are separate from the source-code license. See `docs/MUSIC.md`.

## Development tools

Playwright and sharp are test-only dependencies with their own upstream licenses. They are not bundled in the Windows runtime ZIP. Browser music tests generate a short sine-wave fixture locally and redistribute no recording.
