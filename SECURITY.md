# Security and privacy

HKDownloader is distributed as a Windows executable and may download third-party media tools over HTTPS. It checks their SHA-256 digests and maintains an integrity manifest; checksums reduce accidental or malicious modification risk but are not a substitute for code signing.

Official HKDownloader releases include `HKDownloader.exe.sha256`. Check the SHA-256 hash if you want to confirm that your download matches the published file.

**Optional browser access**: HKDownloader first attempts public extraction. When the optional feature is enabled and access restrictions are detected, it may ask permission to access a local browser session. It does not ask for the browser password. Cookies may be sent to the corresponding website by yt-dlp when accessing authenticated media. Don't publish logs containing private URLs, cookies or personal data.

Report security issues privately to HKCorp where a private contact channel is available; otherwise avoid posting secrets in public GitHub issues.
