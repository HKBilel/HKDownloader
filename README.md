# HKDownloader

**Free Windows desktop downloader for video, audio and playlists.**

HKDownloader is a free-to-use desktop application by **HKCorp**, powered by [yt-dlp](https://github.com/yt-dlp/yt-dlp). It features a clean, monochrome glass-inspired interface, light and dark themes, and nine interface languages.

**v1.0.0 Stable · Windows 10/11 x64 · 100% free · No subscriptions or premium tiers**

### [Download HKDownloader v1.0.0 for Windows (ZIP)](https://github.com/HKBilel/HKDownloader/releases/download/v1.0.0/HKDownloader-Windows-x64-v1.0.0.zip)

[View the release](https://github.com/HKBilel/HKDownloader/releases/tag/v1.0.0) · [Verify the ZIP (SHA-256)](https://github.com/HKBilel/HKDownloader/releases/download/v1.0.0/HKDownloader-Windows-x64-v1.0.0.zip.sha256) · [Report an issue](https://github.com/HKBilel/HKDownloader/issues)

![HKDownloader desktop application, light and dark interface](https://github.com/user-attachments/assets/0f73cc30-2613-459f-8e07-a9f95f6610f0)

## Features

- **Video and audio:** MP4 video, MP3 audio and playlist downloads where supported by yt-dlp and the source website.
- **Quality options:** Best available quality by default, with selectable 720p, 1080p, 1440p and 4K options. Higher qualities may be available in Best mode when supported.
- **Website support:** Works with websites supported by yt-dlp, including YouTube, Twitch, Instagram and TikTok, subject to provider restrictions.
- **Optional browser sessions:** With your permission, HKDownloader can use an existing browser session for content your account is authorised to access. It does not bypass paywalls or memberships.
- **Modern interface:** Monochrome Desktop Acrylic-inspired appearance on compatible Windows systems, light/dark themes and a responsive layout.
- **Nine languages:** English, Spanish, French, German, Portuguese, Italian, Arabic, Japanese and Chinese, including right-to-left support for Arabic.
- **Download controls:** Configurable destination, progress display, cancellation and optional manual end trimming.
- **Tool management:** First-run installation prompt and update checks for yt-dlp, FFmpeg and Deno. The external tools are downloaded separately when needed.
- **Completely free:** No paid unlocks, account registration or membership requirement. Patreon support is optional.

## Download and installation

1. **[Download the Windows x64 ZIP](https://github.com/HKBilel/HKDownloader/releases/download/v1.0.0/HKDownloader-Windows-x64-v1.0.0.zip)** from the official v1.0.0 release.
2. Extract `HKDownloader-Windows-x64-v1.0.0.zip` to a folder where your Windows account has write access.
3. Run **`HKDownloader.exe`**.
4. On first launch, a welcome dialog recommends installing **yt-dlp, FFmpeg and Deno** from their official sources. Choose **Install tools** to download them, or **Later** to postpone installation.
5. Paste a supported URL, choose your settings and start downloading. If you postponed tool installation, HKDownloader can install the required tools when you first download or through **Update tools**.

HKDownloader is distributed as a **ZIP application**, not a traditional installer.

### System requirements

- Windows 10 or Windows 11, **64-bit**.
- Microsoft **.NET Framework 4.8**.
- Internet access for downloads, installing tools and update checks.

> **Important:** Under GitHub's release assets, download **`HKDownloader-Windows-x64-v1.0.0.zip`**, not the automatically generated **Source code (zip/tar.gz)** files. The latter contain only this public documentation repository, **not** the HKDownloader application.

## Verify your download

The [v1.0.0 release](https://github.com/HKBilel/HKDownloader/releases/tag/v1.0.0) includes:

- [`HKDownloader-Windows-x64-v1.0.0.zip.sha256`](https://github.com/HKBilel/HKDownloader/releases/download/v1.0.0/HKDownloader-Windows-x64-v1.0.0.zip.sha256) — ZIP integrity checksum.
- [`HKDownloader.exe.sha256`](https://github.com/HKBilel/HKDownloader/releases/download/v1.0.0/HKDownloader.exe.sha256) — executable integrity checksum.

You can inspect a file's hash on Windows in PowerShell:

```powershell
Get-FileHash .\HKDownloader-Windows-x64-v1.0.0.zip -Algorithm SHA256
```

Compare the resulting hash with the value in the corresponding `.sha256` file from the **same release**. A matching checksum verifies file integrity; it is not equivalent to a digital signature.

## Privacy and responsible use

HKDownloader does **not** require an account or request your account password. Access to an existing browser session is optional and requires permission. When used, yt-dlp may read session cookies locally and send them to the corresponding website to perform authorised requests.

Do not publicly share logs containing private URLs, cookies or personal information. Only download media you are authorised to access, and comply with applicable law and website terms.

[Security information](SECURITY.md) · [Report a bug](https://github.com/HKBilel/HKDownloader/issues)

## Limitations of v1.0.0

Available sites, formats, resolutions and watermark-free sources depend on the platform and yt-dlp. Some sites may require authentication or restrict downloads.

A persistent download history, multi-item download queue and simultaneous downloads of **different videos** are **not included** in v1.0.0. Up to four parallel fragments may be used for compatible formats; this is not the same as multiple concurrent videos.

## Updates and support

HKDownloader checks the [official GitHub releases](https://github.com/HKBilel/HKDownloader/releases) for application updates and can check/download supported external tools. Get new builds only from the official repository.

HKDownloader is completely free. If you wish to support continued development, [optional donations through Patreon](https://www.patreon.com/cw/HKBilel/membership) are welcome and **do not unlock any extra features**.

## Licence and acknowledgements

**Copyright © 2026 HKCorp. All rights reserved.**

HKDownloader is **free-to-use proprietary software**, not open source. The source code is not distributed in this public repository. See the [End User Licence](EULA.md) for usage terms.

HKDownloader uses independent tools under their own licences: [yt-dlp](https://github.com/yt-dlp/yt-dlp), [FFmpeg](https://ffmpeg.org/) and [Deno](https://deno.com/). See [Third-Party Notices](THIRD_PARTY_NOTICES.md).

[Release notes](RELEASE_NOTES-v1.0.0.md) · [Changelog](CHANGELOG.md) · [Official v1.0.0 release](https://github.com/HKBilel/HKDownloader/releases/tag/v1.0.0)
