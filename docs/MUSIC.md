# Optional local music

The release is silent until you add a local audio file. No commercial recording, extracted game audio, or soundtrack download is included.

## Identify the intended track

- **Track:** Elden Ring
- **Artist / composer:** Tsukasa Saitoh
- **Album:** Elden Ring (Original Soundtrack)
- **Release date shown in the catalog:** September 9, 2022
- **Suggested search:** `Tsukasa Saitoh Elden Ring Original Soundtrack`
- **Catalog reference:** [Apple Music](https://music.apple.com/us/song/1642388052)
- **Album service links:** [Official soundtrack landing page](https://soundtracks.lnk.to/eldenring)

This is the base game's title/menu theme. Do not confuse it with the Shadow of the Erdtree or Nightreign soundtrack albums. The credit above identifies the selected track; it does not attribute the entire album to a single composer.

## Add your file

1. Obtain a local MP3 through a legitimate source you may use.
2. Rename the file to `menu-music.mp3`.
3. Put it in `media/menu-music.mp3` inside the extracted release, next to the executable's directory.
4. Start the launcher again. No rebuild is needed.

The launcher does not turn streaming-service links or protected offline downloads into audio files. Another local MP3 also works; it must be longer than 15 seconds.

## Playback behavior

Every pass starts at **15 seconds**. When a pass ends, the next pass starts at **15 seconds again**, skipping the opening 15 seconds every time. Volume fades in at startup. Continuing into the actual app fades the audio out, stops playback, and releases the audio element before handoff. Closing the startup window also stops its audio. Warm launches play no music.

The code defaults are `startSeconds: 15`, `volume: 0.22`, `fadeInMs: 1000`, and `fadeOutMs: 1800` in `scripts/build_scene.py`. Change these and rebuild only if you want different behavior.

## Distribution

The track is not part of this project's MIT license. Keep recordings out of pull requests, repository history, release archives, and issue attachments unless you hold the necessary redistribution rights. [FromSoftware's music notice](https://www.fromsoftware.jp/ww/faq-other.html) is not a blanket license to redistribute soundtrack files.
