# HKDownloader v1.0.0 — Security notes

## Third-party tool integrity

HKDownloader downloads yt-dlp, Deno and FFmpeg from their respective release sources over HTTPS, verifies publisher-provided SHA-256 values before installing archives or executables, and records a SHA-256 for each extracted/installed executable in the local `tools/integrity.json` manifest.

Before downloads, HKDownloader checks the manifest and installed executables. A missing or changed executable or manifest triggers a new verified download rather than executing the untrusted copy. The process runner checks a managed executable again immediately before launching it. The downloader refuses to proceed if it cannot repair or verify the tools. Existing installs without a manifest are migrated by downloading fresh verified copies.

**Limitations:** The manifest is a local record, **not a cryptographic signature**. Malware with permission to edit both the executable and the manifest can forge both. Protect the installation folder with appropriate OS permissions and keep Windows and your security software up to date. There is also a small time-of-check/time-of-use window between checking the binary and launching it. Published checksums and HTTPS do not eliminate all third-party supply-chain risks.

## Build pipeline

All four external GitHub Actions are pinned to full immutable upstream commit IDs, verified against their official GitHub `v4`/`v3` tags when this patch was authored. Refresh pins deliberately after reviewing the corresponding release and its permissions.

## Local files and publishing

`.gitignore` excludes the application's default platform download directories (including `Facebook/` and `Other/`) and common partial download files. Avoid storing private downloads in the source tree. Always review `git status` and commit diffs before publishing. Existing files already tracked by Git are **not** removed by adding `.gitignore` rules.

## Validation

The source includes offline integrity regression tests, activated by `HKDownloader.exe --self-test`, for valid and modified tools, damaged manifests, approved release URLs, and insecure/untrusted download URLs. Compiling and running these tests requires Windows 10/11 x64 and .NET Framework 4.8. Static checks alone do not demonstrate that live downloads or third-party binaries are safe.
