using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

[assembly: AssemblyTitle("HKDownloader")]
[assembly: AssemblyDescription("Desktop downloader powered by yt-dlp")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace HKDownloader
{
    public class Options
    {
        public string Folder { get; set; }
        public bool Audio { get; set; }
        public int Height { get; set; }
        public string Bitrate { get; set; }
        public bool Playlist { get; set; }
        public string Platform { get; set; }
        public bool PreferCleanVideo { get; set; }
        public int TrimEndingSeconds { get; set; }
        public string Language { get; set; }
        public string Theme { get; set; }
        public bool? AutoCheckUpdates { get; set; }
        public Options()
        {
            Height = 1080; Bitrate = "320K"; Playlist = true;
            Platform = "youtube"; PreferCleanVideo = true; TrimEndingSeconds = 0;
            Language = "auto"; Theme = "system"; AutoCheckUpdates = true;
        }
    }

    public static class PlatformPalette
    {
        // These brand accents are fixed; no user-edited color state.
        public static string ColorFor(string platform)
        {
            switch ((platform ?? "").ToLowerInvariant())
            {
                case "twitch": return "#9146FF";
                case "kick": return "#53FC18";
                case "instagram": return "#E1306C";
                case "tiktok": return "#25F4EE";
                case "facebook": return "#0866FF";
                case "youtube": return "#FF0000";
                default: return "#959AA5"; // Neutral, until a supported platform is detected
            }
        }
    }

    public static class Commands
    {
        public static string Quote(string value)
        {
            // CommandLineToArgvW / Microsoft CRT rules. No shell is involved.
            var s = new StringBuilder("\""); int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') { s.Append('\\', slashes * 2 + 1); s.Append('"'); }
                else { s.Append('\\', slashes); s.Append(c); }
                slashes = 0;
            }
            s.Append('\\', slashes * 2); s.Append('"'); return s.ToString();
        }
        public static string Join(IEnumerable<string> args) { return String.Join(" ", args.Select(Quote)); }
        public static string ValidateUrl(string input)
        {
            string value = (input ?? "").Trim(); Uri uri;
            if (value.Length > 8192 || value.Any(Char.IsWhiteSpace) || !Uri.TryCreate(value, UriKind.Absolute, out uri)
                || (uri.Scheme != "https" && uri.Scheme != "http") || String.IsNullOrEmpty(uri.Host))
                throw new ArgumentException(Loc.T("urlInvalid"));
            return value;
        }
        // Choose the site from the link itself. Unknown hosts remain available to yt-dlp,
        // with neutral branding rather than pretending they belong to YouTube.
        public static string DetectPlatform(string input)
        {
            Uri uri;
            if (!Uri.TryCreate((input ?? "").Trim(), UriKind.Absolute, out uri)
                || (uri.Scheme != "https" && uri.Scheme != "http")) return "other";
            string host = uri.DnsSafeHost.ToLowerInvariant();
            if (host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal)
                || host == "youtu.be" || host == "youtube-nocookie.com"
                || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal)) return "youtube";
            if (host == "twitch.tv" || host == "www.twitch.tv"
                || host == "m.twitch.tv" || host == "go.twitch.tv") return "twitch";
            if (host == "kick.com" || host == "www.kick.com") return "kick";
            if (host == "instagram.com" || host == "www.instagram.com" || host == "m.instagram.com") return "instagram";
            if (host == "tiktok.com" || host == "www.tiktok.com" || host == "m.tiktok.com"
                || host == "vm.tiktok.com" || host == "vt.tiktok.com") return "tiktok";
            if (host == "facebook.com" || host == "www.facebook.com" || host == "m.facebook.com"
                || host == "mbasic.facebook.com" || host == "web.facebook.com"
                || host == "fb.watch" || host == "www.fb.watch") return "facebook";
            return "other";
        }

        public static string ValidateTwitchVodUrl(string input)
        {
            string value = ValidateUrl(input);
            Uri uri = new Uri(value);
            string host = uri.DnsSafeHost.ToLowerInvariant();
            if (host != "twitch.tv" && host != "www.twitch.tv" && host != "m.twitch.tv" && host != "go.twitch.tv")
                throw new ArgumentException(Loc.T("twitchInvalid"));
            if (!Regex.IsMatch(uri.AbsolutePath, @"^/(?:videos/[0-9]+|[a-zA-Z0-9_]+/v(?:ideo)?/[0-9]+)/?$", RegexOptions.IgnoreCase))
                throw new ArgumentException(Loc.T("twitchInvalid"));
            return value;
        }
        public static string ValidatePlatformUrl(string input, string platform)
        {
            string value = ValidateUrl(input);
            if (platform == "youtube") return value;
            if (platform == "twitch") return ValidateTwitchVodUrl(value);
            Uri uri = new Uri(value);
            string host = uri.DnsSafeHost.ToLowerInvariant();
            bool valid = false;
            if (platform == "kick") valid = host == "kick.com" || host == "www.kick.com";
            if (platform == "instagram") valid = host == "instagram.com" || host == "www.instagram.com" || host == "m.instagram.com";
            if (platform == "tiktok") valid = host == "tiktok.com" || host == "www.tiktok.com" || host == "m.tiktok.com" || host == "vm.tiktok.com" || host == "vt.tiktok.com";
            if (platform == "facebook") valid = host == "facebook.com" || host == "www.facebook.com" ||
                host == "m.facebook.com" || host == "mbasic.facebook.com" || host == "web.facebook.com" ||
                host == "fb.watch" || host == "www.fb.watch";
            if (!valid) throw new ArgumentException(Loc.T("platformInvalid"));
            return value;
        }
        public static List<string> Download(Options o, string url, string tools)
        {
            if (!new[] { 720, 1080, 1440, 2160 }.Contains(o.Height)) throw new ArgumentException(Loc.T("heightInvalid"));
            if (!new[] { "320K", "256K", "192K", "128K" }.Contains(o.Bitrate)) throw new ArgumentException(Loc.T("bitrateInvalid"));
            string platform = DetectPlatform(url);
            string validated = platform == "other" ? ValidateUrl(url) : ValidatePlatformUrl(url, platform);
            if (o.TrimEndingSeconds < 0 || o.TrimEndingSeconds > 5)
                throw new ArgumentException("Ending trim must be between 0 and 5 seconds.");
            bool twitch = platform == "twitch";
            bool collection = !twitch && platform != "kick" && platform != "facebook";
            string category = platform == "twitch" ? "Twitch VODs" :
                platform == "kick" ? "Kick" : platform == "instagram" ? "Instagram" :
                platform == "tiktok" ? "TikTok" : platform == "facebook" ? "Facebook" :
                platform == "youtube" ? "YouTube" : "Other";
            string suffix = o.Audio ? " [" + o.Bitrate + "]" : " [%(height)sp]";
            string name = "%(title).100s [%(id)s]" + suffix + ".%(ext)s";
            if (collection && o.Playlist) name = "%(playlist_title,playlist_id|Single Videos).70s/%(playlist_index&{:03d} - |)s" + name;
            name = category + "/" + name;
            var args = new List<string> {
                "--ignore-config", "--no-plugin-dirs", "--no-colors", "--encoding", "utf-8",
                "--newline", "--progress", "--progress-delta", "0.3", "--no-simulate",
                "--no-abort-on-error", "--abort-on-unavailable-fragments", "--continue", "--no-overwrites",
                "--windows-filenames", "--trim-filenames", "180", "--socket-timeout", "30",
                "--retries", "5", "--fragment-retries", "5", "--ffmpeg-location", tools,
                "--no-js-runtimes", "--js-runtimes", "deno:" + Path.Combine(tools, "deno.exe"),
                "--paths", o.Folder, "--output", name,
                "--print", "before_dl:APP_ITEM|%(.{title,playlist_index,playlist_count,n_entries})j",
                "--print", "after_move:APP_DONE|%(filepath)j",
                "--progress-template", "download:APP_PROGRESS|%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s",
                "--progress-template", "postprocess:APP_POST|%(progress.status)s"
            };
            if (!collection) args.Add("--no-playlist");
            else if (o.Playlist) args.Add("--yes-playlist");
            else args.AddRange(new[] { "--no-playlist", "--playlist-items", "1" });
            if (o.Audio) args.AddRange(new[] { "-f", "bestaudio/best", "--extract-audio", "--audio-format", "mp3", "--audio-quality", o.Bitrate });
            else
            {
                string standard = "bv*[height<=" + o.Height + "]+ba/b[height<=" + o.Height + "]";
                // Prefer documented non-watermarked formats when the source labels them.
                // Always fall back to the ordinary selector if no clean format is available.
                string clean = "bv*[height<=" + o.Height + "][format_note!*=\"watermarked\"]+ba"
                    + "/b[height<=" + o.Height + "][format_note!*=\"watermarked\"]";
                string selector = o.PreferCleanVideo && (platform == "tiktok" || platform == "instagram")
                    ? clean + "/" + standard : standard;
                args.AddRange(new[] { "-f", selector,
                    "-S", "res,fps,vcodec:h264,acodec:aac", "--merge-output-format", "mp4", "--remux-video", "mp4" });
            }
            args.Add("--"); args.Add(validated); return args;
        }
    }

    // A job owns ONLY this application's child processes. Cancelling kills yt-dlp and ffmpeg together.
    public sealed class ProcessJob : IDisposable
    {
        private IntPtr handle;
        [StructLayout(LayoutKind.Sequential)] struct BasicLimits { public long a, b; public uint flags; public UIntPtr c, d; public uint e; public UIntPtr f; public uint g, h; }
        [StructLayout(LayoutKind.Sequential)] struct IoCounters { public ulong a, b, c, d, e, f; }
        [StructLayout(LayoutKind.Sequential)] struct ExtendedLimits { public BasicLimits basic; public IoCounters io; public UIntPtr a, b, c, d; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetInformationJobObject(IntPtr job, int info, IntPtr data, uint length);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr value);
        public ProcessJob()
        {
            handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero) throw new Win32Exception();
            var limits = new ExtendedLimits(); limits.basic.flags = 0x2000; // KILL_ON_JOB_CLOSE
            int size = Marshal.SizeOf(limits); IntPtr ptr = Marshal.AllocHGlobal(size);
            try { Marshal.StructureToPtr(limits, ptr, false); if (!SetInformationJobObject(handle, 9, ptr, (uint)size)) throw new Win32Exception(); }
            catch { Dispose(); throw; }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        public void Add(Process p) { if (!AssignProcessToJobObject(handle, p.Handle)) throw new Win32Exception(); }
        public void Dispose() { var h = Interlocked.Exchange(ref handle, IntPtr.Zero); if (h != IntPtr.Zero) CloseHandle(h); }
    }

    public static class Runner
    {
        public static async Task<int> Run(string exe, IEnumerable<string> args, string working, Action<string> onLine, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // Detect a modified yt-dlp / ffmpeg / ffprobe / Deno binary before launch.
            // Tools managed by the installer have verified fingerprints in integrity.json.
            ToolIntegrity.AssertExecutable(exe, token);
            using (var job = new ProcessJob())
            using (var p = new Process())
            {
                p.StartInfo = new ProcessStartInfo(exe, Commands.Join(args)) {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                    RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8, WorkingDirectory = working
                };
                p.StartInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                p.Start();
                try { job.Add(p); }
                catch { try { p.Kill(); } catch { } throw; }
                using (token.Register(job.Dispose))
                {
                    Task output = ReadLines(p.StandardOutput, onLine);
                    Task error = ReadLines(p.StandardError, onLine);
                    await Task.Run((Action)p.WaitForExit);
                    await Task.WhenAll(output, error);
                    token.ThrowIfCancellationRequested();
                    return p.ExitCode;
                }
            }
        }
        static async Task ReadLines(StreamReader reader, Action<string> onLine)
        {
            string line; while ((line = await reader.ReadLineAsync()) != null) onLine(line);
        }
    }

    public sealed class Installer : IDisposable
    {
        readonly string root; readonly HttpClient client; readonly Action<string, double> report;
        public Installer(string toolRoot, Action<string, double> progress)
        {
            root = toolRoot; report = progress;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            client = new HttpClient(); client.Timeout = TimeSpan.FromMinutes(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HKDownloader/1.0");
        }
        public void Dispose() { client.Dispose(); }
        public bool Ready { get { return ToolIntegrity.Ready(root); } }
        public async Task Ensure(bool update, CancellationToken token)
        {
            Directory.CreateDirectory(root);
            // Unverified existing tools are never trusted merely because files exist.
            // Missing/corrupted fingerprints trigger a fresh download and validation.
            if (update || !ToolIntegrity.Verify(root, token, "yt-dlp.exe"))
                await Github("yt-dlp/yt-dlp", "yt-dlp.exe", "yt-dlp", false, token);
            if (update || !ToolIntegrity.Verify(root, token, "deno.exe"))
                await Github("denoland/deno", "deno-x86_64-pc-windows-msvc.zip", "deno", true, token);
            if (update || !ToolIntegrity.Verify(root, token, "ffmpeg.exe", "ffprobe.exe"))
                await Ffmpeg(token);
            if (!Ready) throw new IOException(Loc.T("toolIntegrityFail"));
        }
        public static bool TrustedGithubAsset(string url, string repo, string filename)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                uri.Scheme != "https" || uri.Port != 443 || uri.UserInfo.Length != 0 ||
                !uri.DnsSafeHost.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
                !String.IsNullOrEmpty(uri.Query) || !String.IsNullOrEmpty(uri.Fragment)) return false;
            // Only official release assets in the two explicitly supported repositories.
            if (repo != "yt-dlp/yt-dlp" && repo != "denoland/deno") return false;
            string prefix = "/" + repo + "/releases/download/";
            string path = uri.AbsolutePath;
            return path.StartsWith(prefix, StringComparison.Ordinal) &&
                path.Length > prefix.Length + filename.Length + 1 &&
                path.EndsWith("/" + filename, StringComparison.Ordinal);
        }
        async Task<string> GetText(string url, CancellationToken token)
        {
            using (var response = await client.GetAsync(url, token)) { response.EnsureSuccessStatusCode(); return await response.Content.ReadAsStringAsync(); }
        }
        async Task Github(string repo, string filename, string label, bool zipped, CancellationToken token)
        {
            report(Loc.F("search", label), -1);
            var json = new JavaScriptSerializer();
            var release = json.Deserialize<Dictionary<string, object>>(await GetText("https://api.github.com/repos/" + repo + "/releases/latest", token));
            string version = Convert.ToString(release["tag_name"]);
            string installed = Path.Combine(root, label + ".version");
            if (File.Exists(installed) && File.ReadAllText(installed) == version &&
                ToolIntegrity.Verify(root, token, label + ".exe")) return;
            var assets = ((System.Collections.IEnumerable)release["assets"]).Cast<Dictionary<string, object>>().ToArray();
            var asset = assets.FirstOrDefault(a => Convert.ToString(a["name"]) == filename);
            if (asset == null) throw new IOException(Loc.F("assetMissing", label));
            string assetUrl = Convert.ToString(asset["browser_download_url"]);
            if (!TrustedGithubAsset(assetUrl, repo, filename)) throw new IOException(Loc.T("httpsRequired"));
            string expected = asset.ContainsKey("digest") ? Convert.ToString(asset["digest"]) : "";
            if (expected.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) expected = expected.Substring(7);
            else
            {
                string checksumName = label == "yt-dlp" ? "SHA2-256SUMS" : filename + ".sha256sum";
                var sumsAsset = assets.FirstOrDefault(a => Convert.ToString(a["name"]) == checksumName);
                if (sumsAsset == null) throw new IOException(Loc.F("checksumMissing", label));
                string sumsUrl = Convert.ToString(sumsAsset["browser_download_url"]);
                if (!TrustedGithubAsset(sumsUrl, repo, checksumName)) throw new IOException(Loc.T("httpsRequired"));
                string sums = await GetText(sumsUrl, token);
                expected = ParseChecksum(sums, label == "yt-dlp" ? filename : null);
            }
            if (!Regex.IsMatch(expected ?? "", "^[0-9a-fA-F]{64}$")) throw new IOException(Loc.F("checksumInvalid", label));
            string temp = NewStage();
            try
            {
                string downloaded = Path.Combine(temp, filename);
                await Fetch(assetUrl, downloaded, label, token);
                Verify(downloaded, expected, token);
                if (zipped) { ExtractOne(downloaded, "deno.exe", Path.Combine(temp, "deno.exe"), token); Promote(Path.Combine(temp, "deno.exe"), Path.Combine(root, "deno.exe")); }
                else Promote(downloaded, Path.Combine(root, filename));
                ToolIntegrity.Record(root, token, label + ".exe");
                File.WriteAllText(installed, version);
            }
            finally { Cleanup(temp); }
        }
        async Task Ffmpeg(CancellationToken token)
        {
            report(Loc.F("search", "FFmpeg"), -1);
            string version = (await GetText("https://www.gyan.dev/ffmpeg/builds/release-version", token)).Trim();
            if (!Regex.IsMatch(version, "^[0-9]+(\\.[0-9]+){1,3}$")) throw new IOException(Loc.T("ffmpegVersion"));
            string installed = Path.Combine(root, "ffmpeg.version");
            if (File.Exists(installed) && File.ReadAllText(installed) == version &&
                ToolIntegrity.Verify(root, token, "ffmpeg.exe", "ffprobe.exe")) return;
            string url = "https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-" + version + "-essentials_build.zip";
            string checksum = ParseChecksum(await GetText(url + ".sha256", token), null);
            string temp = NewStage();
            try
            {
                string zip = Path.Combine(temp, "ffmpeg.zip");
                await Fetch(url, zip, "FFmpeg", token); Verify(zip, checksum, token);
                ExtractOne(zip, "ffmpeg.exe", Path.Combine(temp, "ffmpeg.exe"), token);
                ExtractOne(zip, "ffprobe.exe", Path.Combine(temp, "ffprobe.exe"), token);
                Promote(Path.Combine(temp, "ffmpeg.exe"), Path.Combine(root, "ffmpeg.exe"));
                Promote(Path.Combine(temp, "ffprobe.exe"), Path.Combine(root, "ffprobe.exe"));
                ToolIntegrity.Record(root, token, "ffmpeg.exe", "ffprobe.exe");
                File.WriteAllText(installed, version);
            }
            finally { Cleanup(temp); }
        }
        public static string ParseChecksum(string text, string filename)
        {
            foreach (string line in text.Split('\n'))
            {
                var match = Regex.Match(line.Trim(), "^([a-fA-F0-9]{64})(?:\\s+\\*?(.+))?$");
                if (match.Success && (filename == null || match.Groups[2].Value.Trim() == filename)) return match.Groups[1].Value;
            }
            throw new IOException(Loc.T("manifestInvalid"));
        }
        async Task Fetch(string url, string target, string label, CancellationToken token)
        {
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) throw new IOException(Loc.T("httpsRequired"));
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode(); long total = response.Content.Headers.ContentLength ?? -1;
                using (var source = await response.Content.ReadAsStreamAsync())
                using (var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                {
                    byte[] buffer = new byte[81920]; long read = 0; int count; var clock = Stopwatch.StartNew();
                    while ((count = await source.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, count, token); read += count;
                        if (read > 600L * 1024 * 1024) throw new IOException(Loc.T("packageTooLarge"));
                        if (clock.ElapsedMilliseconds > 200) { report(Loc.F("prepare", label, (read / 1048576.0).ToString("0.0")), total > 0 ? 100.0 * read / total : -1); clock.Restart(); }
                    }
                    if (total >= 0 && read != total) throw new IOException(Loc.F("downloadIncomplete", label));
                }
            }
            token.ThrowIfCancellationRequested(); report(Loc.F("verify", label), -1);
        }
        public static void Verify(string path, string expected, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); string hash;
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (!String.Equals(hash, expected, StringComparison.OrdinalIgnoreCase)) throw new IOException(Loc.T("hashMismatch"));
            token.ThrowIfCancellationRequested();
        }
        public static void ExtractOne(string zip, string name, string output, CancellationToken token)
        {
            using (var archive = ZipFile.OpenRead(zip))
            {
                var entries = archive.Entries.Where(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (entries.Length != 1 || entries[0].Length > 500L * 1024 * 1024) throw new IOException(Loc.F("archiveMissing", name));
                token.ThrowIfCancellationRequested();
                // Use the application's fixed target, NEVER the archive-supplied path.
                using (var input = entries[0].Open()) using (var target = File.Create(output)) input.CopyTo(target);
                token.ThrowIfCancellationRequested();
            }
        }
        string NewStage() { string path = Path.Combine(root, ".preparing-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
        void Cleanup(string path)
        {
            string full = Path.GetFullPath(path); string expected = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(expected, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(full).StartsWith(".preparing-")) return;
            try { Directory.Delete(full, true); } catch { }
        }
        static void Promote(string source, string target) { if (File.Exists(target)) File.Replace(source, target, null); else File.Move(source, target); }
    }

    public sealed class Controller
    {
        readonly Window window; readonly string root, tools;
        readonly TextBox url, folder, log;
        readonly RadioButton video, audio;
        readonly ComboBox quality, bitrate, languageBox, themeBox, trimEnding;
        readonly CheckBox playlist, autoCheck, cleanSource;
        readonly TextBlock status, detail, count, engine, hint;
        readonly ProgressBar progress;
        readonly Button download, cancel, update, browse, paste, appUpdate;
        readonly Expander expander;
        CancellationTokenSource cancellation;
        bool busy, closeWhenDone, checkingAppUpdate;
        int completed, errors;
        string lastFile, platform = "other", theme = "system", language = "auto";
        readonly List<string> downloadedFiles = new List<string>();
        readonly JavaScriptSerializer json = new JavaScriptSerializer();

        public Controller(Window w, string storage)
        {
            window = w; root = storage;
            string englishTools = Path.Combine(root, "tools");
            string legacyTools = Path.Combine(root, "herramientas");
            tools = !Directory.Exists(englishTools) && Directory.Exists(legacyTools) ? legacyTools : englishTools;
            url = Get<TextBox>("UrlBox"); folder = Get<TextBox>("FolderBox"); log = Get<TextBox>("LogBox");
            video = Get<RadioButton>("VideoRadio"); audio = Get<RadioButton>("AudioRadio");
            quality = Get<ComboBox>("QualityBox"); bitrate = Get<ComboBox>("AudioQualityBox");
            languageBox = Get<ComboBox>("LanguageBox"); themeBox = Get<ComboBox>("ThemeBox");
            playlist = Get<CheckBox>("PlaylistBox"); autoCheck = Get<CheckBox>("AutoUpdateCheckBox");
            cleanSource = Get<CheckBox>("CleanSourceBox"); trimEnding = Get<ComboBox>("TrimEndingBox");
            status = Get<TextBlock>("StatusText"); detail = Get<TextBlock>("DetailText");
            count = Get<TextBlock>("CountText"); engine = Get<TextBlock>("EngineText");
            hint = Get<TextBlock>("QualityHint");
            progress = Get<ProgressBar>("ProgressBar");
            download = Get<Button>("DownloadButton"); cancel = Get<Button>("CancelButton");
            update = Get<Button>("UpdateButton"); appUpdate = Get<Button>("AppUpdateButton"); browse = Get<Button>("BrowseButton");
            paste = Get<Button>("PasteButton");
            expander = Get<Expander>("LogExpander");

            Load();
            download.Click += async delegate { await Start(false); };
            update.Click += async delegate { await Start(true); };
            cancel.Click += delegate { Cancel(); };
            paste.Click += delegate { try { if (Clipboard.ContainsText()) url.Text = Clipboard.GetText().Trim(); } catch { Show(Loc.T("clipboardFail")); } };
            browse.Click += delegate { ChooseFolder(); };
            Get<Button>("OpenButton").Click += delegate { OpenFolder(); };
            Get<Button>("CopyLogButton").Click += delegate { try { Clipboard.SetText(log.Text); } catch { Show(Loc.T("copyFail")); } };
            video.Checked += delegate { ChangeFormat(); };
            audio.Checked += delegate { ChangeFormat(); };
            // No manual platform selector: entering/pasting a link updates the theme and logo.
            url.TextChanged += delegate {
                if (busy) return;
                platform = Commands.DetectPlatform(url.Text);
                ApplyTheme();
            };
            cleanSource.Checked += delegate { if (!busy) Save(); };
            cleanSource.Unchecked += delegate { if (!busy) Save(); };
            trimEnding.SelectionChanged += delegate { if (!busy) Save(); };
            Get<Button>("NavAboutButton").Click += delegate { Show(Loc.T("aboutApp") + "\n" + "v1.0.0\n" + Loc.T("poweredByYtDlp") + "\n" + "github.com/HKBilel/HKDownloader"); };
            Get<Hyperlink>("SupportedSitesLink").RequestNavigate += delegate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
                e.Handled = true;
                try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
                catch (Exception ex) { Show(Loc.T("openSupportedSitesFailed") + " " + ex.Message); }
            };
            appUpdate.Click += async delegate { await CheckForAppUpdates(true); };
            autoCheck.Checked += delegate { if (!busy) Save(); };
            autoCheck.Unchecked += delegate { if (!busy) Save(); };
            window.Loaded += async delegate { if (autoCheck.IsChecked == true) await CheckForAppUpdates(false); };
            languageBox.SelectionChanged += delegate {
                if (busy) return;
                language = SelectedTag(languageBox, "auto");
                Loc.Select(language); ApplyLocale(); Save();
            };
            themeBox.SelectionChanged += delegate {
                if (busy) return;
                theme = SelectedTag(themeBox, "system"); ApplyTheme(); Save();
            };
            window.Closing += Closing;
            Loc.Select(language); ApplyLocale(); ApplyTheme(); UpdatePlatform(); UpdateEngine();
        }

        T Get<T>(string name) where T : class
        {
            T control = window.FindName(name) as T;
            if (control == null)
                throw new InvalidOperationException("Missing or invalid UI element '" + name +
                    "' (expected " + typeof(T).Name + "). Check MainWindow.xaml.");
            return control;
        }
        static string SelectedTag(ComboBox box, string fallback)
        {
            ComboBoxItem item = box.SelectedItem as ComboBoxItem;
            return item == null ? fallback : Convert.ToString(item.Tag);
        }
        static void SelectTag(ComboBox box, string tag)
        {
            foreach (ComboBoxItem item in box.Items)
                if (String.Equals(Convert.ToString(item.Tag), tag, StringComparison.OrdinalIgnoreCase))
                { box.SelectedItem = item; return; }
            box.SelectedIndex = 0;
        }
        void Show(string message)
        {
            MessageBox.Show(window, message, Loc.T("engineTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        void Load()
        {
            Options settings = new Options { Folder = DefaultDownloads() };
            try {
                string path = Path.Combine(root, "settings.json");
                if (!File.Exists(path)) path = Path.Combine(root, "ajustes.json"); // Read-only migration.
                if (File.Exists(path)) settings = json.Deserialize<Options>(File.ReadAllText(path));
            } catch { }
            if (settings == null) settings = new Options();
            platform = "other"; // Old selected-platform preference is intentionally ignored.
            theme = new[] { "dark", "light", "system" }.Contains(settings.Theme) ? settings.Theme : "system";
            language = settings.Language == "auto" || Loc.Supports(settings.Language ?? "") ? settings.Language : "auto";
            if (String.IsNullOrEmpty(language)) language = "auto";
            folder.Text = String.IsNullOrWhiteSpace(settings.Folder) ? DefaultDownloads() : settings.Folder;
            audio.IsChecked = settings.Audio; video.IsChecked = !settings.Audio; playlist.IsChecked = settings.Playlist;
            SelectTag(quality, settings.Height.ToString(CultureInfo.InvariantCulture));
            SelectTag(bitrate, settings.Bitrate ?? "320K");
            cleanSource.IsChecked = settings.PreferCleanVideo;
            SelectTag(trimEnding, new[] { 0, 1, 2, 3, 5 }.Contains(settings.TrimEndingSeconds) ? settings.TrimEndingSeconds.ToString(CultureInfo.InvariantCulture) : "0");
            SelectTag(languageBox, language); SelectTag(themeBox, theme); autoCheck.IsChecked = settings.AutoCheckUpdates != false;
        }
        Options Current()
        {
            return new Options {
                Folder = folder.Text.Trim(), Audio = audio.IsChecked == true,
                Height = Int32.Parse(SelectedTag(quality, "1080"), CultureInfo.InvariantCulture),
                Bitrate = SelectedTag(bitrate, "320K"), Playlist = playlist.IsChecked == true,
                Platform = platform, PreferCleanVideo = cleanSource.IsChecked == true,
                TrimEndingSeconds = Int32.Parse(SelectedTag(trimEnding, "0"), CultureInfo.InvariantCulture),
                Theme = theme, Language = language, AutoCheckUpdates = autoCheck.IsChecked == true
            };
        }
        void Save()
        {
            try { Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "settings.json"), json.Serialize(Current()), Encoding.UTF8); }
            catch (Exception e) { Append(Loc.T("savePrefsFail") + e.Message); }
        }
        void Text(string name, string key) { Get<TextBlock>(name).Text = Loc.T(key); }
        void ButtonText(Button button, string key) { button.Content = Loc.T(key); }
        void ApplyLocale()
        {
            // Arabic uses a right-to-left layout, while URLs, file paths and logs stay left-to-right.
            window.FlowDirection = Loc.Language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            url.FlowDirection = folder.FlowDirection = log.FlowDirection = FlowDirection.LeftToRight;
            Text("SubtitleText", "subtitle"); Text("LanguageLabel", "language");
            Text("ThemeLabel", "theme"); Text("PoweredByText", "poweredByYtDlp");
            Text("LinkHeading", "linkHeading"); Text("FormatHeading", "formatHeading");
            Text("PlaylistHint", "playlistHint"); Text("FolderHeading", "folderHeading");
            Text("PlatformNote", "platformNote");
            Text("AutoDetectSidebarLabel", "autoDetectSidebar");
            var sitesLink = Get<Hyperlink>("SupportedSitesLink");
            sitesLink.Inlines.Clear(); sitesLink.Inlines.Add(new Run(Loc.T("allSupportedSites")));
            Text("ShortOptionsTitle", "shortOptions");
            Text("CleanSourceHint", "cleanSourceHint");
            Text("TrimEndingLabel", "trimEndingLabel");
            Text("TrimEndingHint", "trimEndingHint");
            cleanSource.Content = Loc.T("cleanSource");
            ((ComboBoxItem)trimEnding.Items[0]).Content = Loc.T("disabledOption");
            ButtonText(paste, "paste"); ButtonText(browse, "browse"); ButtonText(download, "download");
            ButtonText(cancel, "cancel"); ButtonText(Get<Button>("OpenButton"), "open");
            ButtonText(Get<Button>("CopyLogButton"), "copy"); ButtonText(update, "update");
            ButtonText(appUpdate, "appUpdateCheck");
            ButtonText(Get<Button>("NavAboutButton"), "navAbout");
            Text("AutoUpdateLabel", "appAutoCheck");
            video.Content = Loc.T("video"); audio.Content = Loc.T("audio");
            playlist.Content = Loc.T("playlist"); expander.Header = Loc.T("log");
            ((ComboBoxItem)languageBox.Items[0]).Content = Loc.T("auto");
            ((ComboBoxItem)themeBox.Items[0]).Content = Loc.T("system");
            ((ComboBoxItem)themeBox.Items[1]).Content = Loc.T("light");
            ((ComboBoxItem)themeBox.Items[2]).Content = Loc.T("dark");
            url.ToolTip = Loc.T("autoDetectHelp");
            Get<TextBlock>("LinkHelp").Text = Loc.T("autoDetectHelp");
            ChangeFormat();
            UpdatePlatform();
            if (!busy) { status.Text = Loc.T("ready"); detail.Text = Loc.T("readyDetail"); }
            UpdateEngine();
        }
        void ChangeFormat()
        {
            bool isAudio = audio.IsChecked == true;
            quality.Visibility = isAudio ? Visibility.Collapsed : Visibility.Visible;
            bitrate.Visibility = isAudio ? Visibility.Visible : Visibility.Collapsed;
            hint.Text = Loc.T(isAudio ? "qualityAudio" : "qualityVideo");
            Get<StackPanel>("ShortVideoOptions").Visibility =
                !isAudio && (platform == "tiktok" || platform == "instagram")
                ? Visibility.Visible : Visibility.Collapsed;
        }
        // Return color contrast for text placed over the active accent.
        static MediaColor Contrast(MediaColor c)
        {
            double l = (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;
            return l > 0.61 ? MediaColor.FromRgb(20, 23, 30) : MediaColor.FromRgb(255, 255, 255);
        }
        static MediaColor Blend(MediaColor background, MediaColor foreground, double ratio)
        {
            return MediaColor.FromRgb(
                (byte)Math.Round(background.R * (1 - ratio) + foreground.R * ratio),
                (byte)Math.Round(background.G * (1 - ratio) + foreground.G * ratio),
                (byte)Math.Round(background.B * (1 - ratio) + foreground.B * ratio));
        }
        static MediaColor Hex(string value) { return (MediaColor)ColorConverter.ConvertFromString(value); }
        void SetBrush(string key, MediaColor color) { window.Resources[key] = new SolidColorBrush(color); }
        bool IsLight()
        {
            if (theme == "light") return true;
            if (theme == "dark") return false;
            try
            {
                using (var r = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    if (r != null) return Convert.ToInt32(r.GetValue("AppsUseLightTheme", 0)) != 0;
            } catch { }
            return false;
        }
        void ApplyTheme()
        {
            bool light = IsLight();
            string platformColor = PlatformPalette.ColorFor(platform);
            MediaColor main = Hex(platformColor);
            SetBrush("CanvasBrush", Hex(light ? "#F5F3EE" : "#121315"));
            SetBrush("SurfaceBrush", Hex(light ? "#FCFAF6" : "#1B1C1F"));
            SetBrush("SurfaceAltBrush", Hex(light ? "#EAE7E1" : "#27292D"));
            SetBrush("InputBrush", Hex(light ? "#F7F5F0" : "#141517"));
            SetBrush("TextBrush", Hex(light ? "#1A1C1E" : "#F2F2F0"));
            SetBrush("MutedBrush", Hex(light ? "#62615D" : "#B2B4B9"));
            SetBrush("BorderBrushApp", Hex(light ? "#D6D2CA" : "#3B3D42"));
            SetBrush("AccentBrush", main);
            SetBrush("AccentSoftBrush", Blend(Hex(light ? "#FCFAF6" : "#1B1C1F"), main, light ? 0.14 : 0.22));
            SetBrush("OnAccentBrush", Contrast(main));
            window.Background = (Brush)window.Resources["CanvasBrush"];
            window.Foreground = (Brush)window.Resources["TextBrush"];
            UpdatePlatform();
        }
        void UpdatePlatform()
        {
            bool single = platform == "twitch" || platform == "kick" || platform == "facebook";
            Get<StackPanel>("PlaylistPanel").Visibility = single ? Visibility.Collapsed : Visibility.Visible;
            Get<TextBlock>("PlatformNote").Visibility =
                platform == "other" || platform == "youtube" ? Visibility.Collapsed : Visibility.Visible;
            Get<TextBlock>("PlatformNote").Text = Loc.T("platformNote");
            Get<TextBlock>("PlatformDescription").Text = platform == "other"
                ? Loc.T("autoDetectReady")
                : Loc.F("autoDetectFound", platform == "tiktok" ? "TikTok" :
                    platform == "youtube" ? "YouTube" : platform == "facebook" ? "Facebook" :
                    platform == "instagram" ? "Instagram" : platform == "twitch" ? "Twitch" : "Kick");
            Get<TextBlock>("LinkHelp").Text = Loc.T("autoDetectHelp");
            url.ToolTip = Loc.T("autoDetectHelp");
            ChangeFormat();
            if (platform == "other") Branding.Apply(window);
            else Branding.SetPlatform(window, platform);
        }
        static string DefaultDownloads()
        {
            // Respect the Windows Downloads known folder, including relocated user profiles.
            IntPtr value; Guid id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out value) == 0)
            { try { return Path.Combine(Marshal.PtrToStringUni(value), "HKDownloader"); } finally { Marshal.FreeCoTaskMem(value); } }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "HKDownloader");
        }
        [DllImport("shell32.dll")] static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
        void ChooseFolder()
        {
            using (var dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = Loc.T("folderDialog"); dialog.ShowNewFolderButton = true;
                if (Directory.Exists(folder.Text)) dialog.SelectedPath = folder.Text;
                if (dialog.ShowDialog(new NativeOwner(new System.Windows.Interop.WindowInteropHelper(window).Handle)) == Forms.DialogResult.OK)
                { folder.Text = dialog.SelectedPath; Save(); }
            }
        }
        sealed class NativeOwner : Forms.IWin32Window
        {
            public IntPtr Handle { get; private set; }
            public NativeOwner(IntPtr handle) { Handle = handle; }
        }
        void OpenFolder()
        {
            try
            {
                string target = folder.Text.Trim();
                if (!Directory.Exists(target)) { Show(Loc.T("folderMissing")); return; }
                Process.Start(new ProcessStartInfo { FileName = Path.GetFullPath(target), UseShellExecute = true });
            }
            catch (Exception e) { Show(Loc.T("folderOpenFail") + e.Message); }
        }
        void UpdateEngine()
        {
            using (var installer = new Installer(tools, delegate { }))
                engine.Text = Loc.T(installer.Ready ? "toolsReady" : "toolsFirst");
        }
        async Task CheckForAppUpdates(bool manual)
        {
            if (busy || checkingAppUpdate) return;
            checkingAppUpdate = true;
            appUpdate.IsEnabled = false;
            try
            {
                var release = await AppUpdater.CheckAsync();
                if (release == null || !AppUpdater.Newer(release.Number))
                {
                    if (manual) Show(Loc.T("appUpToDate"));
                    return;
                }
                string notes = String.IsNullOrWhiteSpace(release.Notes) ? "" : "\n\n" + release.Notes.Trim();
                if (notes.Length > 650) notes = notes.Substring(0, 650) + "…";
                if (MessageBox.Show(window, Loc.F("appUpdateAvailable", release.Tag) + notes,
                    Loc.T("engineTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                status.Text = Loc.T("appUpdateDownloading");
                detail.Text = Loc.T("appUpdateDownloadingDetail");
                string staged = await AppUpdater.DownloadVerifiedAsync(release);
                AppUpdater.InstallAfterExit(staged);
                window.Close();
            }
            catch (Exception ex)
            {
                if (manual) Show(Loc.T("appUpdateFailed") + "\n" + ex.Message);
                else Append(Loc.T("appUpdateFailed") + " " + ex.Message);
            }
            finally { checkingAppUpdate = false; appUpdate.IsEnabled = !busy; }
        }

        void SetBusy(bool value)
        {
            busy = value;
            download.IsEnabled = update.IsEnabled = browse.IsEnabled = paste.IsEnabled = !value;
            appUpdate.IsEnabled = !value && !checkingAppUpdate; autoCheck.IsEnabled = !value;
            url.IsEnabled = folder.IsEnabled = quality.IsEnabled = bitrate.IsEnabled = !value;
            video.IsEnabled = audio.IsEnabled = playlist.IsEnabled = !value;
            cleanSource.IsEnabled = trimEnding.IsEnabled = !value;
            languageBox.IsEnabled = themeBox.IsEnabled = !value;
            cancel.IsEnabled = value;
            if (!value) progress.IsIndeterminate = false;
        }
        void Append(string message)
        {
            if (message.Length > 6000) message = message.Substring(0, 6000);
            if (log.Text.Length > 90000) log.Text = log.Text.Substring(log.Text.Length - 60000);
            log.AppendText(message + Environment.NewLine); log.ScrollToEnd();
        }
        void UI(Action action)
        {
            if (!window.Dispatcher.HasShutdownStarted) window.Dispatcher.BeginInvoke(action);
        }
        async Task Start(bool updateOnly)
        {
            if (busy) return;
            string link = "";
            Options options = Current();
            if (!updateOnly)
            {
                try
                {
                    options.Platform = Commands.DetectPlatform(url.Text);
                    link = options.Platform == "other" ? Commands.ValidateUrl(url.Text)
                        : Commands.ValidatePlatformUrl(url.Text, options.Platform);
                    if (options.Audio || (options.Platform != "tiktok" && options.Platform != "instagram"))
                        options.TrimEndingSeconds = 0;
                    if (String.IsNullOrWhiteSpace(options.Folder) || !Path.IsPathRooted(options.Folder))
                        throw new ArgumentException(Loc.T("folderInvalid"));
                    options.Folder = Path.GetFullPath(options.Folder);
                    Directory.CreateDirectory(options.Folder);
                    string probe = Path.Combine(options.Folder, ".yt-test-" + Guid.NewGuid().ToString("N"));
                    using (File.Create(probe)) { }
                    File.Delete(probe);
                }
                catch (Exception e) { Show(e.Message); return; }
            }
            Save(); completed = errors = 0; lastFile = null; downloadedFiles.Clear(); log.Clear(); count.Text = "";
            SetBusy(true); cancellation = new CancellationTokenSource(); var token = cancellation.Token;
            status.Text = Loc.T("startTools"); detail.Text = Loc.T("startToolsDetail");
            progress.IsIndeterminate = true; progress.Value = 0;
            try
            {
                using (var installer = new Installer(tools, (text, value) => UI(delegate {
                    status.Text = text; progress.IsIndeterminate = value < 0;
                    if (value >= 0) progress.Value = Math.Max(0, Math.Min(100, value));
                })))
                    await Task.Run(() => installer.Ensure(updateOnly, token));
                token.ThrowIfCancellationRequested(); UpdateEngine();
                if (updateOnly)
                {
                    status.Text = Loc.T("toolsUpdated"); detail.Text = Loc.T("toolsUpdatedDetail");
                    progress.Value = 100; return;
                }
                status.Text = Loc.T("checking"); detail.Text = Loc.T("checkingDetail"); progress.IsIndeterminate = true;
                var args = Commands.Download(options, link, tools);
                Append(Loc.T("start") + " · " + (options.Audio ? "MP3 " + options.Bitrate : "MP4 " + Loc.T("upTo") + " " + options.Height + "p")
                    + " · " + (options.Platform == "twitch" ? "Twitch VOD" : options.Platform == "kick" ? "Kick" : options.Platform == "instagram" ? "Instagram" : options.Platform == "tiktok" ? "TikTok" : options.Platform == "facebook" ? "Facebook" : Loc.T(options.Playlist ? "fullPlaylist" : "oneVideo")));
                int code = await Runner.Run(Path.Combine(tools, "yt-dlp.exe"), args, root, line => UI(() => HandleLine(line)), token);
                await window.Dispatcher.InvokeAsync(delegate { }, System.Windows.Threading.DispatcherPriority.Background);
                token.ThrowIfCancellationRequested(); progress.IsIndeterminate = false;
                if (code == 0 && errors == 0 && completed > 0 && options.TrimEndingSeconds > 0)
                {
                    status.Text = Loc.T("postprocessing"); progress.IsIndeterminate = true;
                    foreach (string source in downloadedFiles.ToArray())
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            string output = await MediaCleanup.TrimAsync(source, options.TrimEndingSeconds, tools, root,
                                token, message => UI(() => Append(message)));
                            lastFile = output;
                            Append(Loc.F("trimCreated", output));
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { errors++; Append(Loc.F("trimFailed", Path.GetFileName(source)) + " " + ex.Message); }
                    }
                    progress.IsIndeterminate = false;
                }
                if (code == 0 && errors == 0 && completed > 0)
                {
                    progress.Value = 100; status.Text = Loc.T("complete");
                    detail.Text = completed == 1 ? Loc.T("completeDetailOne") : Loc.F("completeDetailMany", completed);
                }
                else
                {
                    progress.Value = 0; status.Text = completed > 0 ? Loc.T("issues") : Loc.T("failed");
                    detail.Text = completed > 0 ? Loc.F("issuesDetail", completed) : Loc.T("failedDetail");
                    expander.IsExpanded = true;
                    Append(Loc.F("processExit", code, completed));
                }
            }
            catch (OperationCanceledException)
            {
                status.Text = Loc.T("cancelled"); detail.Text = Loc.T("cancelledDetail");
                Append(Loc.T("cancelledLog")); progress.Value = 0;
            }
            catch (Exception e)
            {
                status.Text = Loc.T("unexpected"); detail.Text = Loc.T("unexpectedDetail");
                Append(e.Message + (e.InnerException != null ? "\n" + e.InnerException.Message : ""));
                expander.IsExpanded = true; progress.Value = 0;
            }
            finally
            {
                SetBusy(false); cancellation.Dispose(); cancellation = null; UpdateEngine();
                if (closeWhenDone) window.Close();
            }
        }
        void HandleLine(string line)
        {
            if (line.StartsWith("APP_PROGRESS|"))
            {
                string[] parts = line.Split('|'); if (parts.Length < 4) return;
                double percent; string value = parts[1].Trim().TrimEnd('%');
                if (Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out percent))
                { progress.IsIndeterminate = false; progress.Value = Math.Max(0, Math.Min(100, percent)); }
                status.Text = Loc.F("downloading", parts[1].Trim());
                detail.Text = Loc.F("speed", parts[2].Trim(), parts[3].Trim()); return;
            }
            if (line.StartsWith("APP_ITEM|"))
            {
                try
                {
                    var item = json.Deserialize<Dictionary<string, object>>(line.Substring(9));
                    string title = item.ContainsKey("title") ? Convert.ToString(item["title"]) : "Video";
                    detail.Text = title; progress.IsIndeterminate = true; progress.Value = 0;
                    object index, total; item.TryGetValue("playlist_index", out index);
                    if (!item.TryGetValue("playlist_count", out total) || total == null) item.TryGetValue("n_entries", out total);
                    status.Text = index != null ? Loc.F("item", index, total != null ? Loc.F("of", total) : "") : Loc.T("preparingItem");
                    Append(status.Text + " · " + title);
                }
                catch { Append(line); }
                return;
            }
            if (line.StartsWith("APP_DONE|"))
            {
                try
                {
                    string path = json.Deserialize<string>(line.Substring(9));
                    if (!String.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        lastFile = path; completed++; downloadedFiles.Add(path); count.Text = Loc.F("readyCount", completed);
                        Append(Loc.F("saved", path));
                    }
                    else { errors++; Append(Loc.F("unconfirmed", path)); }
                }
                catch { errors++; Append(line); }
                return;
            }
            if (line.StartsWith("APP_POST|"))
            {
                status.Text = Loc.T("postprocessing"); detail.Text = Loc.T("postDetail");
                progress.IsIndeterminate = true; return;
            }
            if (line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase)) errors++;
            if (!String.IsNullOrWhiteSpace(line)) Append(line);
        }
        void Cancel()
        {
            if (cancellation != null)
            {
                cancel.IsEnabled = false; status.Text = Loc.T("cancelled"); cancellation.Cancel();
            }
        }
        void Closing(object sender, CancelEventArgs e)
        {
            if (busy)
            {
                e.Cancel = true;
                if (MessageBox.Show(window, Loc.T("closing"), Loc.T("engineTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                { closeWhenDone = true; Cancel(); }
            }
            else Save();
        }
    }


    // The executable and taskbar use the supplied yellow HK icon.
    // The in-app platform logos remain independent and dynamically colored.
    public static class Branding
    {
        // Each logo is rendered at build time from the supplied SVG artwork.
        // The sidebar/header logos use platform accents, but the Windows titlebar,
        // taskbar and executable keep the yellow HK app icon across all sites.
        private static readonly Dictionary<string, BitmapFrame> cache =
            new Dictionary<string, BitmapFrame>(StringComparer.OrdinalIgnoreCase);

        private static BitmapFrame LoadLogo(string resourceName)
        {
            BitmapFrame logo;
            if (cache.TryGetValue(resourceName, out logo)) return logo;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Embedded HKDownloader logo missing: " + resourceName);
                logo = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                logo.Freeze();
                cache.Add(resourceName, logo);
                return logo;
            }
        }

        private static void SetImages(Window window, BitmapFrame logo)
        {
            var sidebar = window.FindName("SidebarLogo") as Image;
            var header = window.FindName("HeaderLogo") as Image;
            if (sidebar == null || header == null)
                throw new InvalidOperationException("HKDownloader logo containers missing from MainWindow.xaml.");
            sidebar.Source = logo;
            header.Source = logo;
        }

        public static void Apply(Window window)
        {
            var neutral = LoadLogo("HKDownloader.Logo.png");
            window.Icon = LoadLogo("HKDownloader.AppIcon.png");
            SetImages(window, neutral);
        }

        public static void SetPlatform(Window window, string platform)
        {
            // Explicit allowlist avoids loading arbitrary manifest resource names.
            switch (platform)
            {
                case "youtube": case "twitch": case "kick":
                case "instagram": case "tiktok": case "facebook": break;
                default: throw new ArgumentException("Unknown platform for logo: " + platform);
            }
            SetImages(window, LoadLogo("HKDownloader.Logo." + platform + ".png"));
        }
    }

    public static class Program
    {
        [STAThread] public static int Main(string[] args)
        {
            int fixtureResult; if (SelfTest.TryFixture(args, out fixtureResult)) return fixtureResult;
            if (args.Length > 0 && args[0] == "--self-test") return SelfTest.Run();
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                using (var mutex = new Mutex(false, "Local\\HKDownloader_" + Hash(root)))
                {
                    bool acquired; try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) { MessageBox.Show("HKDownloader is already running from this folder.", "HKDownloader"); return 0; }
                    try
                    {
                        var app = new Application();
                        using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml"))
                        {
                            Window window = (Window)XamlReader.Load(resource);
                            Branding.Apply(window);
                            new Controller(window, root);
                            return app.Run(window);
                        }
                    }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            catch (Exception e)
            {
                try
                {
                    string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HKDownloader");
                    Directory.CreateDirectory(logFolder);
                    File.WriteAllText(Path.Combine(logFolder, "startup-error.log"),
                        DateTime.Now.ToString("o") + Environment.NewLine + e.ToString(), Encoding.UTF8);
                }
                catch { /* Diagnostics should never cause a second startup error. */ }
                MessageBox.Show("Could not start HKDownloader.\n\n" + e.Message +
                    "\n\nIf it persists, see: %LOCALAPPDATA%\\HKDownloader\\startup-error.log",
                    "HKDownloader", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
        }
        static string Hash(string value) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value.ToLowerInvariant()))).Replace("-", "").Substring(0, 20); }
    }
}