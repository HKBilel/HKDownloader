# Security and privacy

HKDownloader is distributed as a Windows executable and may download third-party media tools over HTTPS. It checks their SHA-256 digests and maintains an integrity manifest; checksums reduce accidental or malicious modification risk but are not a substitute for code signing.

Official HKDownloader releases include `HKDownloader.exe.sha256`. Check the SHA-256 hash if you want to confirm that your download matches the published file.

## Optional browser access

HKDownloader first attempts public extraction. When the optional feature is enabled and access restrictions are detected, it may ask permission to access a local browser session. It does not ask for the browser password. Cookies may be sent to the corresponding website by yt-dlp when accessing authenticated media.

Do not share logs containing private URLs, cookies, access tokens or other personal data.

## Reporting a security vulnerability

**Do not report exploitable vulnerabilities, credentials, cookies, tokens or private logs in public GitHub issues.**

If the repository offers a **Report a vulnerability** option under [Security > Advisories](https://github.com/HKBilel/HKDownloader/security/advisories), use it to submit a private report to the maintainer. This depends on GitHub's private vulnerability reporting feature being enabled.

If that option is unavailable, do not disclose sensitive details publicly. Request a private contact method from the maintainer without including vulnerability details, or use an existing trusted private contact channel.

For ordinary, non-sensitive bugs or feature requests, use [GitHub Issues](https://github.com/HKBilel/HKDownloader/issues).

**Repository maintainer:** To accept private reports directly through GitHub, enable **Private vulnerability reporting** in the repository's security settings and verify that the reporting option is visible.
