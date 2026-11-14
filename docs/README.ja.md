# HKDownloader v1.0.0 — 日本語

## 動作環境
Windows 10/11（64ビット）、.NET Framework 4.8、インターネット接続。GitHub Releases から `HKDownloader.exe` を入手してください。ソースからビルドする場合は Windows で `Build-HKDownloader.cmd` を実行します。

## 主な機能
- URL入力欄は1つ。サイトを自動判別し、プラットフォームに応じて色とロゴが切り替わります。
- YouTube、Twitch、Kick、Instagram、TikTok、Facebook、その他 yt-dlp が対応するサイト。
- MP4：720p／1080p／1440p／2160p（4K）までの画質制限。MP3：128／192／256／320 kbps。実際の品質は配信元によります。
- 対応プレイリストの一括保存、保存先選択、進行状況、キャンセル、ログ表示。
- TikTok／Instagram：提供されている場合は透かしのないソースを優先。末尾1／2／3／5秒を削除した別のMP4を作成できます。元ファイルは保持されます。動画に焼き込まれた透かしの除去や終了画面の自動検出は保証されません。
- 9言語対応。外観はシステム／ライト／ダークから選べます。

## 使い方
URLを貼り付け、MP4またはMP3、品質、保存先を選択してダウンロードします。必要に応じて yt-dlp、FFmpeg、Deno が取得されます。

## セキュリティ
ツールのダウンロード時と実行前に整合性を確認しますが、ローカルのハッシュ記録は電子署名ではありません。ブラウザーのCookieやパスワードは取り込みません。権利を有するコンテンツのみ保存してください。[セキュリティ](SECURITY.md)も参照してください。

## テスト
Windows で `HKDownloader.exe --self-test` を実行します。
対応サイト一覧：https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
