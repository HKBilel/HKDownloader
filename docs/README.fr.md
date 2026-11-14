# HKDownloader v1.0.0 — Français

## Configuration
Windows 10/11 (64 bits), .NET Framework 4.8 et connexion Internet. Téléchargez `HKDownloader.exe` depuis GitHub Releases. Pour compiler le code source sur Windows, exécutez `Build-HKDownloader.cmd`.

## Fonctionnalités
- Un seul champ pour toutes les URL : la plateforme et ses couleurs sont détectées automatiquement.
- YouTube, Twitch, Kick, Instagram, TikTok, Facebook et d'autres sites compatibles avec yt-dlp.
- Vidéo MP4 : limites de 720p, 1080p, 1440p ou 2160p (4K). Audio MP3 : 128, 192, 256 ou 320 kb/s. La qualité dépend de la source.
- Listes de lecture compatibles, choix du dossier, progression, annulation et journal.
- TikTok/Instagram : préférence pour les sources sans filigrane si elles existent et copie MP4 optionnelle sans les 1, 2, 3 ou 5 dernières secondes. L'original est conservé. Aucun retrait garanti des filigranes incrustés et aucune détection automatique de la fin.
- Neuf langues et thèmes Système, Clair ou Sombre.

## Utilisation
Collez le lien, choisissez MP4 ou MP3, la qualité, le dossier puis cliquez sur **Télécharger**. yt-dlp, FFmpeg et Deno sont téléchargés si nécessaire.

## Sécurité
Les téléchargements des outils sont vérifiés et leurs empreintes locales contrôlées. Une empreinte locale n'est pas une signature numérique. Aucune importation de cookies ou de mots de passe. Ne téléchargez que les contenus autorisés. Voir [Sécurité](SECURITY.md).

## Tests
Sous Windows, exécutez `HKDownloader.exe --self-test`.
Sites pris en charge : https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
