# HKDownloader v1.0.0 — English

## Requirements
- Windows 10/11 (64-bit), .NET Framework 4.8 and an Internet connection.
- Download `HKDownloader.exe` from the GitHub Releases page. The source ZIP contains code rather than a compiled application.
- To compile from source, run `Build-HKDownloader.cmd` on Windows; `Launch-HKDownloader.cmd` compiles the app when necessary and starts it.

## Features
- One universal URL field. The website is identified automatically, with platform-dependent colors and logos.
- YouTube, Twitch, Kick, Instagram, TikTok, Facebook and other websites supported by yt-dlp.
- MP4 video quality limits: 720p, 1080p, 1440p and 2160p (4K); MP3 bitrate choices: 128, 192, 256 or 320 kbps. Actual quality depends on the source.
- Download supported playlists, choose a destination, see progress, cancel downloads and view logs.
- TikTok/Instagram: prefer clean sources when offered by the site; optionally create an additional MP4 with the last 1, 2, 3 or 5 seconds removed. Original files are preserved. No guaranteed watermark removal or automatic outro detection.
- Interface languages: English, Spanish, French, German, Portuguese, Italian, Arabic, Japanese and Simplified Chinese. Appearance: System, Light or Dark.

## How to use
1. Paste a supported link into the universal input field.
2. Select MP4 or MP3, quality and download folder.
3. Choose playlist mode when supported, then click **Download**.
4. yt-dlp, FFmpeg and Deno are downloaded automatically from their configured sources when needed.

## Security and updates
- Tool downloads are checked for integrity, and local tool fingerprints are checked again before execution. Local fingerprints are not digital signatures.
- **Update tools** manages yt-dlp/FFmpeg/Deno; **Check updates** checks official HKDownloader releases.
- The program does not import browser cookies, passwords or account credentials. Only download media you have permission to save.
- Details: [Security notes](SECURITY.md).

## Tests
Compile on Windows and run `HKDownloader.exe --self-test`. Logs are written to `self-test-report.txt`.
Official list of supported sites: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
