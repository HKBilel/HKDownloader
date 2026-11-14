# HKDownloader v1.0.0 — 简体中文

## 系统要求
Windows 10/11（64 位）、.NET Framework 4.8 和互联网连接。从 GitHub Releases 下载 `HKDownloader.exe`。如需从源码构建，请在 Windows 上运行 `Build-HKDownloader.cmd`。

## 功能
- 一个通用链接输入框，自动识别平台并切换对应的界面颜色和标志。
- 支持 YouTube、Twitch、Kick、Instagram、TikTok、Facebook 及 yt-dlp 支持的其他网站。
- MP4 视频分辨率上限可选 720p、1080p、1440p、2160p（4K）；MP3 音质可选 128、192、256、320 kbps。实际质量取决于原视频。
- 支持的播放列表、保存目录选择、进度显示、取消和日志。
- TikTok／Instagram：在平台提供时优先选择无水印源；可额外生成删除末尾 1、2、3 或 5 秒的 MP4 副本，原文件保留。无法保证清除嵌入式水印，也不会自动识别片尾。
- 支持九种界面语言，以及系统、浅色和深色主题。

## 使用方法
粘贴链接，选择 MP4 或 MP3、质量和保存目录，然后开始下载。程序会在需要时获取 yt-dlp、FFmpeg 和 Deno。

## 安全与更新
工具下载及运行前会检查完整性，但本地哈希记录不等同于数字签名。程序不导入浏览器 Cookie 或密码。仅下载有权保存的内容。详见[安全说明](SECURITY.md)。

## 测试
在 Windows 上执行 `HKDownloader.exe --self-test`。
支持的网站列表：https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md
