# HKDownloader v1.0.0 — Español

## Requisitos
- Windows 10/11 de 64 bits, .NET Framework 4.8 y conexión a Internet.
- Descarga `HKDownloader.exe` desde GitHub Releases. El ZIP del código fuente no incluye el programa compilado.
- Para compilarlo en Windows, ejecuta `Build-HKDownloader.cmd`. `Launch-HKDownloader.cmd` lo compila si hace falta y lo inicia.

## Funciones
- Un campo universal para enlaces: detecta la plataforma automáticamente y adapta los colores y logotipos.
- Compatible con YouTube, Twitch, Kick, Instagram, TikTok, Facebook y otras webs admitidas por yt-dlp.
- Vídeo MP4 con límite de 720p, 1080p, 1440p o 2160p (4K). Audio MP3 a 128, 192, 256 o 320 kbps. La calidad real depende del vídeo original.
- Descarga de listas compatibles, selección de carpeta, progreso, cancelación y registro de errores.
- TikTok/Instagram: prioriza formatos sin marca de agua cuando estén disponibles; permite crear otra copia MP4 recortando 1, 2, 3 o 5 segundos del final. Conserva el original. No elimina marcas incrustadas ni detecta automáticamente las pantallas finales.
- Nueve idiomas; apariencia según el sistema, clara u oscura.

## Uso
1. Pega un enlace compatible en el campo universal.
2. Selecciona MP4 o MP3, calidad y carpeta de destino.
3. Elige la lista completa si la plataforma lo permite y pulsa **Descargar**.
4. yt-dlp, FFmpeg y Deno se descargarán cuando sean necesarios.

## Seguridad y actualizaciones
- Las herramientas se descargan con comprobaciones de integridad y sus hashes locales se revisan antes de ejecutarlas. El registro local no equivale a una firma digital.
- **Actualizar herramientas** gestiona yt-dlp/FFmpeg/Deno; **Buscar actualizaciones** consulta las versiones de HKDownloader.
- No importa cookies, contraseñas ni credenciales del navegador. Descarga únicamente contenido para el que tengas autorización.
- Consulta [Seguridad](SECURITY.md).

## Pruebas
Compila en Windows y ejecuta `HKDownloader.exe --self-test`; genera `self-test-report.txt`.
Lista oficial de sitios admitidos: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
