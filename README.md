# HKDownloader · v1.0.0

**HKDownloader** is a free desktop application for Windows 10/11 (64-bit), powered by [yt-dlp](https://github.com/yt-dlp/yt-dlp).

**Supported platforms:** YouTube · Twitch · Kick · Instagram · TikTok · Facebook

## Download & installation

1. Download **HKDownloader.exe** from [GitHub Releases](https://github.com/HKBilel/HKDownloader/releases).
2. Place the executable in a writable folder and run it. No installer is required.
3. Paste a supported video or playlist link. HKDownloader detects the platform and adapts its colors automatically.
4. Choose MP3 or MP4, quality, and destination, then download.

An internet connection is required. On first use, HKDownloader may download the tools it needs from their official sources.

## Features

- **One universal download view:** Paste any supported URL; HKDownloader identifies the platform automatically and organizes downloads into platform folders. Unknown hosts are passed to yt-dlp with neutral HK branding.
- **TikTok & Instagram:** Optional preference for unwatermarked formats where the site supplies one. This cannot remove logos already embedded in the video and is not guaranteed to work for every link.
- **Manual end-card trim:** Optionally create an additional MP4 with the last 1, 2, 3 or 5 seconds removed. The original video is always preserved. **This does not automatically detect a platform ending screen**, and choosing too many seconds can remove real content.
- **Windows executable icon:** The yellow HK app icon is used in Windows Explorer and on the taskbar. Inside the app, the header and sidebar logos adapt to the detected platform.

- **Sidebar:** Language selector with a globe icon, appearance control directly below it, automatic update preference and the official [list of yt-dlp supported sites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md).
- **Dropdown menus:** Clear selection, hover and open-state styling for language, appearance, video quality and trimming options.

Official release builds are compiled and tested automatically on Windows. Download the executable from GitHub Releases.

## Security and privacy

- HKDownloader verifies upstream download SHA-256 checksums for yt-dlp, Deno and FFmpeg before installing them. It records SHA-256 fingerprints for the installed tools in `tools/integrity.json` (or the selected tools directory), checks them on startup and before launching a managed tool, and repairs missing or modified tools by downloading them again. An Internet connection is needed when tools must be repaired.
- This local manifest detects accidental changes and simple tampering. **It is not a digital signature** and cannot defend against malware with permission to alter both the tools and their manifest. Keep the application in a directory only trusted users can modify.
- Further details: [Security notes](docs/SECURITY.md).
- GitHub Actions are pinned to verified full commit SHAs. The `.gitignore` excludes generated media folders and partial downloads from accidental Git commits; do not store private downloads inside source control.

## Support the project

HKDownloader is free to use. If you enjoy the project and want to support its development, you can join [HKDownloader on Patreon](https://patreon.com/HKBilel).

## Report an issue

Found a bug or having trouble using HKDownloader? [Report an issue on GitHub](https://github.com/HKBilel/HKDownloader/issues/new).

Please include your Windows and HKDownloader versions, steps to reproduce the problem, and relevant error messages. Do not post private links, passwords, cookies, access tokens or personal information.

## Build from source

On Windows x64 with the .NET Framework 4.8 compiler available, run `Build-HKDownloader.cmd`, then open `HKDownloader.exe`. The Windows icon and taskbar artwork comes from `source/Branding/HKDownloader-AppIcon.svg`, while `source/Branding/HKDownloader.svg` supplies the platform-dependent logos shown within the interface. Windows builds render the ICO and PNG assets from these SVG files locally.

You can run automated integration checks with `HKDownloader.exe --self-test`. The Windows build workflow also runs these tests. Live downloads may still fail if a platform changes its service or restricts particular content.

## Third-party software and acknowledgments

HKDownloader is an independent graphical frontend. It uses external, independently maintained projects:

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — media extraction and downloading.
- [FFmpeg](https://ffmpeg.org/) — media merging, remuxing and conversion.
- [Deno](https://deno.com/) — JavaScript runtime support where needed.

These projects retain their respective authorship, trademarks, licenses and terms. Their use with HKDownloader does not imply endorsement. If redistributing HKDownloader together with third-party binaries, review and comply with the applicable licenses and distribution obligations.

## Legal notices

### Copyright and permitted use

HKDownloader is a general-purpose tool; it does **not** grant permission to download, copy, redistribute or otherwise use third-party content.

Users are responsible for ensuring their downloads and subsequent use comply with applicable copyright and other laws, the rights of content owners, and the relevant platform's terms of service. Download only media you own, have permission to use or are otherwise lawfully entitled to download. **Public availability does not itself authorize downloading or reuse.**

Do not use HKDownloader to infringe others' rights, bypass digital rights management (DRM), or circumvent access controls. The ability of yt-dlp to access a URL is not evidence that a particular use is lawful or permitted.

### Independence and trademarks

HKDownloader is unofficial and is **not affiliated with, sponsored, endorsed, certified or supported by** YouTube, Google, Twitch, Amazon, Kick, Instagram, Meta, TikTok, ByteDance or their affiliates. Platform names, trademarks, logos and other identifiers belong to their respective owners and are used only to describe compatibility. The same applies to third-party software names and marks.

### Software availability, warranty and liability

HKDownloader is provided **"AS IS" and "AS AVAILABLE"**, to the extent permitted by applicable law. Compatibility, uninterrupted access to platforms, permanent availability of downloads and successful media conversion are not guaranteed. External services can change or restrict functionality without notice.

To the extent permitted by applicable law, the developer disclaims warranties and liability for losses resulting from misuse, unauthorized downloading or redistribution, third-party restrictions, service interruptions or software failures. **Nothing here excludes or limits rights, warranties or liabilities that cannot lawfully be excluded or limited.** These notices do not replace applicable law.

### Network access and privacy

The app contacts supported platforms and, when necessary, GitHub and third-party software providers to access media, check for updates or obtain dependencies. Those services may receive request-related technical information and apply their own terms and privacy policies. Avoid including private links, credentials or identifying information in public support reports.

### Source-code licensing

**No license for the HKDownloader source code has been selected yet.** The absence of a license is not permission to copy, modify or redistribute the project. Third-party dependencies have their own separate licenses.
