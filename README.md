# HKDownloader

**A free Windows desktop downloader for video, audio and playlists.**

HKDownloader is a desktop application by **HKCorp**, powered by [yt-dlp](https://github.com/yt-dlp/yt-dlp). It uses a monochrome Glassmorphism interface and supports nine interface languages.

**Free to use · No Premium tiers · No membership required · Proprietary software**

## Download

Get **HKDownloader 1.0.0** from [GitHub Releases](https://github.com/HKBilel/HKDownloader/releases). Choose `HKDownloader-Windows-x64-v1.0.0.zip`, extract it and start `HKDownloader.exe`.

Requirements: Windows 10/11 64-bit, .NET Framework 4.8 and an internet connection for fetching supported tools. The app downloads yt-dlp, FFmpeg and Deno when needed.

> The first release is not published yet; the link will work when v1.0.0 is uploaded.

## Features

- MP4 video, MP3 audio and playlists when supported by the source and yt-dlp.
- Best quality selected by default; selectable 720p, 1080p, 1440p and 4K. Best quality can include higher resolutions when available.
- Automatic detection of supported platforms, including YouTube, Twitch, Instagram, TikTok and others supported by yt-dlp.
- Optional use of an existing browser session for restricted content **only with the user's permission** and only when that account already has access. No bypass of memberships or access restrictions.
- Attempts to prefer unwatermarked sources for some short-form videos when available; manual optional ending trim, preserving the original.
- Adjustable download location, progress, cancellation, light/dark appearance, nine languages and multilingual UI.
- Desktop Acrylic on compatible Windows systems; appearance depends on Windows settings.
- No artificial download rate limit. Up to 4 simultaneous fragments on compatible formats (not multiple simultaneous videos).

**Not included in v1.0.0:** a persistent download history, a multi-item queue or concurrent downloads of different videos. These are future development ideas, not released features.

## Updates

HKDownloader checks [GitHub Releases](https://github.com/HKBilel/HKDownloader/releases) for updates and verifies downloaded executables using their published SHA-256 digest/checksum. Use only the official repository for downloads.

## Optional support

HKDownloader is completely free. You can optionally [support HKCorp on Patreon](https://www.patreon.com/cw/HKBilel/membership); donations unlock no features.

## Copyright and permissions

**Copyright © 2026 HKCorp. All rights reserved.** HKDownloader is **not open-source**. The binaries are provided free to use under the [End User License](EULA.md). Source code is not distributed in this repository.

HKDownloader uses independent upstream tools governed by their own licences; see [Third-Party Notices](THIRD_PARTY_NOTICES.md).

## Privacy, support and limitations

Browser cookies are accessed locally by yt-dlp only after permission and may be transmitted to the associated platform during authenticated requests. Browser security protections may prevent cookie access. HKDownloader does not request account passwords.

Video availability and quality depend on the provider. Use HKDownloader only for media you are authorised to download and comply with applicable law and service terms.

[Report a bug](https://github.com/HKBilel/HKDownloader/issues) · [Security information](SECURITY.md) · [Release notes](CHANGELOG.md)
