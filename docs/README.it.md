# HKDownloader v1.0.0 — Italiano

## Requisiti
Windows 10/11 (64 bit), .NET Framework 4.8 e Internet. Scarica `HKDownloader.exe` da GitHub Releases. Per compilare il sorgente su Windows esegui `Build-HKDownloader.cmd`.

## Funzionalità
- Un unico campo URL con rilevamento automatico della piattaforma e colori dedicati.
- YouTube, Twitch, Kick, Instagram, TikTok, Facebook e altri siti compatibili con yt-dlp.
- Video MP4 con limite 720p, 1080p, 1440p o 2160p (4K); MP3 a 128, 192, 256 o 320 kbps. La qualità dipende dalla sorgente.
- Playlist supportate, cartella di destinazione, avanzamento, annullamento e log.
- TikTok/Instagram: preferenza per sorgenti senza filigrana quando disponibili; possibilità di creare una copia MP4 eliminando gli ultimi 1, 2, 3 o 5 secondi. L'originale resta intatto. Non rimuove necessariamente filigrane incorporate né riconosce automaticamente la schermata finale.
- Nove lingue; temi Sistema, Chiaro e Scuro.

## Utilizzo
Incolla il link, scegli MP4/MP3, qualità e cartella, quindi premi **Download**. yt-dlp, FFmpeg e Deno vengono scaricati quando necessari.

## Sicurezza
Vengono verificati i download degli strumenti e le impronte locali. Le impronte locali non sono firme digitali. Nessuna importazione di cookie o password. Scarica solo contenuti autorizzati. Vedi [Sicurezza](SECURITY.md).

## Test
Su Windows esegui `HKDownloader.exe --self-test`.
Siti supportati: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
