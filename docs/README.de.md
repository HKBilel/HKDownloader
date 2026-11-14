# HKDownloader v1.0.0 — Deutsch

## Voraussetzungen
Windows 10/11 (64 Bit), .NET Framework 4.8 und Internetzugang. `HKDownloader.exe` steht in GitHub Releases bereit. Für einen Quellcode-Build unter Windows `Build-HKDownloader.cmd` ausführen.

## Funktionen
- Ein universelles URL-Feld mit automatischer Plattformerkennung und angepassten Farben.
- YouTube, Twitch, Kick, Instagram, TikTok, Facebook und weitere von yt-dlp unterstützte Seiten.
- MP4-Videogrenzen: 720p, 1080p, 1440p oder 2160p (4K); MP3: 128, 192, 256 oder 320 kbit/s. Die tatsächliche Qualität hängt von der Quelle ab.
- Unterstützte Playlists, Zielordner, Fortschrittsanzeige, Abbrechen und Protokolle.
- TikTok/Instagram: Wasserzeichenfreie Quellen werden bevorzugt, falls verfügbar. Optional wird eine zweite MP4 ohne die letzten 1, 2, 3 oder 5 Sekunden erstellt. Die Originaldatei bleibt erhalten. Keine Garantie für das Entfernen eingebrannter Wasserzeichen oder die automatische Erkennung von Abspännen.
- Neun Sprachen; Design: System, Hell oder Dunkel.

## Verwendung
Link einfügen, MP4/MP3, Qualität und Zielordner wählen und **Download** starten. yt-dlp, FFmpeg und Deno werden bei Bedarf heruntergeladen.

## Sicherheit
Tools und lokale Prüfsummen werden geprüft. Lokale Prüfsummen sind keine digitalen Signaturen. Browser-Cookies und Passwörter werden nicht importiert. Nur berechtigte Inhalte herunterladen. Siehe [Sicherheit](SECURITY.md).

## Tests
Unter Windows `HKDownloader.exe --self-test` ausführen.
Unterstützte Seiten: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
