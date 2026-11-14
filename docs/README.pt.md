# HKDownloader v1.0.0 — Português

## Requisitos
Windows 10/11 (64 bits), .NET Framework 4.8 e Internet. Transfira `HKDownloader.exe` em GitHub Releases. Para compilar no Windows, execute `Build-HKDownloader.cmd`.

## Funcionalidades
- Um campo universal para ligações, com deteção automática da plataforma e cores correspondentes.
- YouTube, Twitch, Kick, Instagram, TikTok, Facebook e outros sites compatíveis com yt-dlp.
- MP4 com limite de 720p, 1080p, 1440p ou 2160p (4K); MP3 a 128, 192, 256 ou 320 kbps. A qualidade depende da fonte.
- Listas de reprodução compatíveis, escolha da pasta, progresso, cancelamento e registos.
- TikTok/Instagram: preferência por fontes sem marca de água, quando disponíveis. É possível criar outra cópia MP4 sem os últimos 1, 2, 3 ou 5 segundos, mantendo o original. Não garante remoção de marcas incorporadas nem deteção automática do ecrã final.
- Nove idiomas; temas Sistema, Claro e Escuro.

## Utilização
Cole uma ligação, escolha MP4/MP3, qualidade e pasta e clique em **Transferir**. yt-dlp, FFmpeg e Deno são obtidos quando necessários.

## Segurança
As ferramentas e as suas impressões digitais locais são verificadas. Não equivalem a assinaturas digitais. Não são importadas palavras-passe nem cookies. Transfira apenas conteúdos autorizados. Consulte [Segurança](SECURITY.md).

## Testes
No Windows execute `HKDownloader.exe --self-test`.
Sites suportados: https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
