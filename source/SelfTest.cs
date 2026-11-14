using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HKDownloader
{
    public static class SelfTest
    {
        static readonly List<string> results = new List<string>();
        static void Assert(bool ok, string name) { if (!ok) throw new Exception(name); results.Add("PASS " + name); }
        static void Throws(Action f, string name) { bool threw = false; try { f(); } catch { threw = true; } Assert(threw, name); }
        public static bool TryFixture(string[] args, out int result)
        {
            result = 0;
            if (args.Length == 0) return false;
            if (args[0] == "--fixture-echo") { using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))) { output.WriteLine(new JavaScriptSerializer().Serialize(args.Skip(1).ToArray())); } return true; }
            if (args[0] == "--fixture-child") { Thread.Sleep(30000); return true; }
            if (args[0] == "--fixture-parent")
            {
                using (var child = Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, "--fixture-child") { UseShellExecute = false, CreateNoWindow = true }))
                { Console.WriteLine("CHILD:" + child.Id); Console.Out.Flush(); Thread.Sleep(30000); }
                return true;
            }
            return false;
        }
        public static int Run()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string temp = Path.Combine(root, ".test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
            int code = 0;
            try { RunTests(temp).GetAwaiter().GetResult(); results.Add("ALL TESTS PASSED"); }
            catch (Exception e) { results.Add("FAIL " + e.ToString()); code = 1; }
            File.WriteAllLines(Path.Combine(root, "self-test-report.txt"), results, Encoding.UTF8);
            string full = Path.GetFullPath(temp);
            if (full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith(".test-")) { try { Directory.Delete(full, true); } catch { } }
            return code;
        }
        static string LogoBackground(System.Windows.Media.ImageSource source)
        {
            var bitmap = source as System.Windows.Media.Imaging.BitmapSource;
            if (bitmap == null) throw new Exception("Platform logo source is not a bitmap");
            var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            var crop = new System.Windows.Media.Imaging.CroppedBitmap(converted,
                new System.Windows.Int32Rect(converted.PixelWidth / 2, converted.PixelHeight / 16, 1, 1));
            var pixel = new byte[4];
            crop.CopyPixels(pixel, 4, 0);
            if (pixel[3] != 255) throw new Exception("Logo background pixel is not opaque");
            return String.Format("#{0:X2}{1:X2}{2:X2}", pixel[2], pixel[1], pixel[0]);
        }

        static async Task RunTests(string temp)
        {
            // Load the real embedded WPF window before any await: the executable's entry thread is STA.
            // This catches XAML structure errors (e.g. a Border with multiple direct children)
            // which a successful C# compilation alone cannot detect.
            using (var resource = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("MainWindow.xaml"))
            {
                Assert(resource != null, "Embedded MainWindow.xaml is present");
                var view = System.Windows.Markup.XamlReader.Load(resource) as System.Windows.Window;
                Assert(view != null, "WPF main window can be parsed and constructed");
                Branding.Apply(view);
                // The taskbar icon uses the yellow artwork, independent of the in-app platform logos.
                var icon = view.Icon as System.Windows.Media.Imaging.BitmapSource;
                Assert(icon != null, "Branded window icon is a bitmap");
                var converted = new System.Windows.Media.Imaging.FormatConvertedBitmap(
                    icon, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                var pixel = new byte[4];
                var sample = new System.Windows.Media.Imaging.CroppedBitmap(
                    converted, new System.Windows.Int32Rect(converted.PixelWidth / 2, converted.PixelHeight / 16, 1, 1));
                sample.CopyPixels(pixel, 4, 0);
                Assert(pixel[0] == 0x15 && pixel[1] == 0xD6 && pixel[2] == 0xFE && pixel[3] == 0xFF,
                    "Windows app icon uses the supplied solid yellow #FED615 artwork");
                var sidebarLogo = view.FindName("SidebarLogo") as System.Windows.Controls.Image;
                var headerLogo = view.FindName("HeaderLogo") as System.Windows.Controls.Image;
                Assert(view.Icon != null, "Window and taskbar have embedded branded titlebar icon");
                Assert(sidebarLogo != null && sidebarLogo.Source != null,
                    "Sidebar displays the embedded HKDownloader logo");
                Assert(headerLogo != null && headerLogo.Source != null,
                    "Main header displays the same embedded HKDownloader logo");
                Assert(Object.ReferenceEquals(sidebarLogo.Source, headerLogo.Source),
                    "Both brand locations reuse identical logo source");
                Assert(view.FindName("AutoUpdateCheckBox") is System.Windows.Controls.CheckBox,
                    "Settings and automatic update controls load");
                Assert(view.FindName("FacebookButton") == null,
                    "Legacy manual platform buttons have been removed");
                Assert(view.FindName("CleanSourceBox") is System.Windows.Controls.CheckBox,
                    "Short-video clean-source option loads");
                Assert(view.FindName("TrimEndingBox") is System.Windows.Controls.ComboBox,
                    "Optional ending trim control loads");
                Assert(view.FindName("PoweredByText") is System.Windows.Controls.TextBlock,
                    "yt-dlp attribution control loads");
                Assert(view.FindName("PlatformDescription") is System.Windows.Controls.TextBlock,
                    "Platform description control loads");
                Assert(view.FindName("SettingsCard") == null &&
                    view.FindName("NavDownloadsButton") == null &&
                    view.FindName("NavSettingsButton") == null,
                    "Redundant bottom settings panel and sidebar navigation buttons are absent");
                var sidebar = view.FindName("SidebarMenu") as System.Windows.Controls.StackPanel;
                var langBox = view.FindName("LanguageBox") as System.Windows.Controls.ComboBox;
                var themeBox = view.FindName("ThemeBox") as System.Windows.Controls.ComboBox;
                var autoUpdateLabel = view.FindName("AutoUpdateLabel") as System.Windows.Controls.TextBlock;
                var supportedLink = view.FindName("SupportedSitesLink") as System.Windows.Documents.Hyperlink;
                Assert(sidebar != null && langBox != null && themeBox != null &&
                    autoUpdateLabel != null && supportedLink != null, "Sidebar controls and official site link load");
                Assert(sidebar.Children.Contains(langBox.Parent as System.Windows.UIElement) &&
                    sidebar.Children.Contains(themeBox.Parent as System.Windows.UIElement),
                    "Language and appearance selectors are both in left sidebar");
                Assert(supportedLink.NavigateUri.AbsoluteUri ==
                    "https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md",
                    "Platform list links to the official yt-dlp supported-sites document");
                foreach (string name in new[] { "LanguageBox", "ThemeBox", "QualityBox", "AudioQualityBox", "TrimEndingBox" })
                {
                    var box = (System.Windows.Controls.ComboBox)view.FindName(name);
                    box.ApplyTemplate();
                    Assert(box.Template.FindName("MenuToggle", box) is
                        System.Windows.Controls.Primitives.ToggleButton,
                        "Dropdown " + name + " has an accessible clickable arrow");
                    Assert(box.Template.FindName("PART_Popup", box) is
                        System.Windows.Controls.Primitives.Popup,
                        "Dropdown " + name + " has working popup template part");
                }


                // Exercise the same controller initialization as Program.Main, not XAML parsing alone.
                // This catches missing named UI references before an EXE can be released.
                var controller = new Controller(view, temp);
                langBox.SelectedIndex = 2; // Español.
                Assert(Loc.Language == "es" && autoUpdateLabel.Text == Loc.T("appAutoCheck"),
                    "Changing sidebar language translates visible controls and update preference");
                Assert(supportedLink.Inlines.FirstInline is System.Windows.Documents.Run &&
                    ((System.Windows.Documents.Run)supportedLink.Inlines.FirstInline).Text == Loc.T("allSupportedSites"),
                    "Supported site link is translated without destroying its hyperlink");
                themeBox.SelectedIndex = 2; // Dark.
                Assert(File.ReadAllText(Path.Combine(temp, "settings.json")).Contains("\"Theme\":\"dark\""),
                    "Sidebar appearance selector persists the chosen theme");
                langBox.SelectedIndex = 1; themeBox.SelectedIndex = 0; // Restore English, system appearance.

                var desc = (System.Windows.Controls.TextBlock)view.FindName("PlatformDescription");
                Assert(!String.IsNullOrWhiteSpace(desc.Text) && desc.Text != "platformYT",
                    "Controller initializes the visible platform description");
                Assert(LogoBackground(view.Icon) == "#FED615",
                    "Window icon uses yellow artwork regardless of platform");
                var logoChecks = new[] {
                    new[] { "https://www.youtube.com/watch?v=abc", "#FF0000", "YouTube" },
                    new[] { "https://www.twitch.tv/videos/123456789", "#9146FF", "Twitch" },
                    new[] { "https://kick.com/channel/videos/abc", "#53FC18", "Kick" },
                    new[] { "https://www.instagram.com/reel/abcd/", "#E1306C", "Instagram" },
                    new[] { "https://www.tiktok.com/@user/video/1234", "#25F4EE", "TikTok" },
                    new[] { "https://www.facebook.com/reel/12345", "#0866FF", "Facebook" }
                };
                var url = (System.Windows.Controls.TextBox)view.FindName("UrlBox");
                var shortOptions = (System.Windows.Controls.StackPanel)view.FindName("ShortVideoOptions");
                foreach (var check in logoChecks)
                {
                    url.Text = check[0]; // Actual TextChanged event drives platform detection.
                    Assert(LogoBackground(headerLogo.Source) == check[1],
                        "Automatic platform logo uses correct color for " + check[2]);
                    Assert(Object.ReferenceEquals(headerLogo.Source, sidebarLogo.Source),
                        "Sidebar and header use same auto-detected logo for " + check[2]);
                    Assert(Object.ReferenceEquals(view.Icon, icon),
                        "EXE taskbar icon never follows the detected platform");
                    Assert(desc.Text.Contains(check[2]),
                        "Header identifies detected platform " + check[2]);
                    bool shortVideo = check[2] == "TikTok" || check[2] == "Instagram";
                    Assert((shortOptions.Visibility == System.Windows.Visibility.Visible) == shortVideo,
                        "Short-video options only shown for TikTok or Instagram");
                }
                url.Text = "https://example.com/watch?id=1";
                Assert(LogoBackground(headerLogo.Source) == "#303030",
                    "Unknown supported site displays neutral graphite brand");
                Assert(Object.ReferenceEquals(view.Icon, icon),
                    "EXE icon remains the yellow app artwork when website is unknown");
                url.Text = "";
                view.Close();
            }
            // Offline regression checks: files are never considered trusted on mere existence.
            string testTools = Path.Combine(temp, "integrity-fixture");
            Directory.CreateDirectory(testTools);
            string[] toolNames = { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" };
            foreach (string name in toolNames)
                File.WriteAllText(Path.Combine(testTools, name), "fixture-" + name);
            Assert(!ToolIntegrity.Ready(testTools),
                "Unregistered binaries cannot pass the integrity check");
            ToolIntegrity.Record(testTools, CancellationToken.None, toolNames);
            Assert(ToolIntegrity.Ready(testTools),
                "SHA-256 manifest validates all four tools");
            ToolIntegrity.AssertExecutable(Path.Combine(testTools, "yt-dlp.exe"), CancellationToken.None);
            File.AppendAllText(Path.Combine(testTools, "yt-dlp.exe"), "-modified");
            Assert(!ToolIntegrity.Ready(testTools) && !ToolIntegrity.Verify(testTools, "yt-dlp.exe"),
                "Tampered yt-dlp.exe is rejected even if file and version already exist");
            Throws(() => ToolIntegrity.AssertExecutable(Path.Combine(testTools, "yt-dlp.exe"), CancellationToken.None),
                "Runner refuses an altered external executable before process creation");
            Assert(ToolIntegrity.Verify(testTools, "ffmpeg.exe", "ffprobe.exe"),
                "Other tools remain valid after one tool is corrupted");
            ToolIntegrity.Record(testTools, CancellationToken.None, "yt-dlp.exe"); // Simulates reinstallation after verified download.
            Assert(ToolIntegrity.Ready(testTools),
                "Reinstalled tool records its new verified digest");
            File.WriteAllText(Path.Combine(testTools, "integrity.json"), "{garbled");
            Assert(!ToolIntegrity.Ready(testTools),
                "Invalid integrity manifest is rejected (fail closed)");
            Assert(Installer.TrustedGithubAsset(
                "https://github.com/yt-dlp/yt-dlp/releases/download/2026.11.01/yt-dlp.exe",
                "yt-dlp/yt-dlp", "yt-dlp.exe"),
                "Official HTTPS GitHub release asset is allowed");
            Assert(!Installer.TrustedGithubAsset(
                "https://github.com.evil.test/yt-dlp/yt-dlp/releases/download/v1/yt-dlp.exe",
                "yt-dlp/yt-dlp", "yt-dlp.exe"),
                "Impersonating GitHub download host is rejected");
            Assert(!Installer.TrustedGithubAsset(
                "https://github.com/attacker/project/releases/download/v1/yt-dlp.exe",
                "yt-dlp/yt-dlp", "yt-dlp.exe"),
                "Untrusted release repository is rejected");
            Assert(!Installer.TrustedGithubAsset(
                "http://github.com/yt-dlp/yt-dlp/releases/download/v1/yt-dlp.exe",
                "yt-dlp/yt-dlp", "yt-dlp.exe"),
                "HTTP tool downloads are rejected");
            Assert(AppUpdater.ParseVersion("v1.0.0").Equals(new Version(1, 0, 0)), "Updater parses v1.0.0 release tag");
            Assert(!AppUpdater.Newer(new Version(1, 0, 0)), "Same release is not an update");
            Assert(!AppUpdater.Newer(new Version(0, 9, 9)), "Older release is not an update");
            Assert(AppUpdater.Newer(new Version(1, 0, 1)), "New v1.0 line detects future 1.0.1 update");
            Assert(AppUpdater.ParseVersion("not-a-tag") == null, "Updater rejects invalid tags");
            Assert(AppUpdater.SafeHash("sha256:" + new string('a', 64)) == new string('a', 64), "Updater accepts a 64-digit SHA-256 checksum");
            Assert(AppUpdater.SafeHash("sha256:invalid") == null, "Updater rejects non-SHA-256 checksums");
            Assert(Commands.ValidateUrl(" https://www.youtube.com/watch?v=abc&list=xyz ") == "https://www.youtube.com/watch?v=abc&list=xyz", "HTTP URL preserved, trimmed and never shell-interpreted");
            Assert(Commands.DetectPlatform("https://www.facebook.com/reel/1") == "facebook",
                "Facebook auto-detected by URL");
            Assert(Commands.DetectPlatform("https://vm.tiktok.com/a1") == "tiktok",
                "Short TikTok domain auto-detected");
            Assert(Commands.DetectPlatform("https://www.instagram.com/reel/a1") == "instagram",
                "Instagram auto-detected");
            Assert(Commands.DetectPlatform("https://youtube.com.evil.example/video") == "other",
                "Impersonating website is never treated as the real brand");
            Assert(Commands.DetectPlatform("https://example.com/watch") == "other",
                "Other yt-dlp-supported hosts are accepted under neutral branding");
            var defaultArgs = Commands.Download(new Options { Folder = temp, Platform = "youtube" },
                "https://www.facebook.com/reel/12345", temp);
            Assert(defaultArgs[defaultArgs.IndexOf("--output") + 1].StartsWith("Facebook/"),
                "Facebook link works regardless of old saved platform selection");
            var cleanArgs = Commands.Download(new Options { Folder = temp, PreferCleanVideo = true },
                "https://www.tiktok.com/@user/video/12345", temp);
            Assert(cleanArgs[cleanArgs.IndexOf("-f") + 1].Contains("format_note!*=") &&
                cleanArgs[cleanArgs.IndexOf("-f") + 1].Contains("/bv*[height<="),
                "Clean-source preference always falls back to normal media formats");
            var normalArgs = Commands.Download(new Options { Folder = temp, PreferCleanVideo = false },
                "https://www.tiktok.com/@user/video/12345", temp);
            Assert(!normalArgs[normalArgs.IndexOf("-f") + 1].Contains("format_note"),
                "Clean-source preference can be disabled");
            Assert(MediaCleanup.TrimArgs("input.mp4", "output.mp4", 9).Contains("9"),
                "Manual trim uses FFmpeg with a valid output duration");
            Throws(() => MediaCleanup.TrimArgs("input.mp4", "out.mp4", 0),
                "Zero remaining duration cannot be trimmed");

            Throws(() => Commands.ValidateUrl("file:///C:/Windows/test"), "Reject file URL");
            Throws(() => Commands.ValidateUrl("--exec calc"), "Reject command option in URL");
            Throws(() => Commands.ValidateUrl("https://example.com/a\nhttps://example.com/b"), "Reject multiple URLs and control whitespace");
            foreach (string locale in new[] {"en", "es", "fr", "de", "pt", "it", "ar", "ja", "zh"})
            {
                Loc.Select(locale);
                Assert(Loc.Language == locale && Loc.T("download") != "download", "Complete language support " + locale);
            }
            Loc.Select("xx");
            Assert(Loc.Language == "en" && Loc.T("download") == "↓   Download", "Unknown Windows language falls back to English");
            Loc.Select("auto");
            var fixedColors = new Dictionary<string, string> {
                {"youtube", "#FF0000"}, {"twitch", "#9146FF"}, {"kick", "#53FC18"},
                {"instagram", "#E1306C"}, {"tiktok", "#25F4EE"}, {"facebook", "#0866FF"} };
            foreach (var pair in fixedColors)
                Assert(PlatformPalette.ColorFor(pair.Key) == pair.Value, "Fixed brand color " + pair.Key);
            var legacy = new JavaScriptSerializer().Deserialize<Options>("{\"Platform\":\"youtube\",\"Accent\":\"#123456\"}");
            Assert(legacy.Platform == "youtube", "Legacy custom color is ignored");
            foreach (string platform in new[] { "kick", "instagram", "tiktok", "facebook" })
            {
                string testUrl = platform == "kick" ? "https://kick.com/my-channel/videos/abcd" :
                    platform == "instagram" ? "https://www.instagram.com/reel/abcd/" :
                    platform == "facebook" ? "https://www.facebook.com/reel/123456789" : "https://www.tiktok.com/@user/video/1234";
                Assert(Commands.ValidatePlatformUrl(testUrl, platform) == testUrl, "Valid " + platform + " URL");
                Throws(() => Commands.ValidatePlatformUrl("https://" + platform + ".com.evil.test/video/1234", platform), "Reject fake " + platform + " domain");
                var platformArgs = Commands.Download(new Options { Folder = temp, Platform = platform }, testUrl, temp);
                Assert(platformArgs[platformArgs.IndexOf("--output") + 1].StartsWith(
                    (platform == "kick" ? "Kick" : platform == "instagram" ? "Instagram" : platform == "facebook" ? "Facebook" : "TikTok") + "/"), "English output folder for " + platform);
            }
            Assert(Commands.ValidatePlatformUrl("https://vm.tiktok.com/1234", "tiktok").Length > 0, "TikTok short link is accepted");
            Assert(Commands.ValidatePlatformUrl("https://m.facebook.com/watch/?v=123456789", "facebook").Length > 0, "Facebook mobile URLs accepted");
            Assert(Commands.ValidatePlatformUrl("https://mbasic.facebook.com/reel/123456789", "facebook").Length > 0, "Facebook basic URLs accepted");
            Assert(Commands.ValidatePlatformUrl("https://fb.watch/abc123/", "facebook").Length > 0, "Facebook short links accepted");
            Throws(() => Commands.ValidatePlatformUrl("https://facebook.com.evil.test/reel/1", "facebook"), "Fake Facebook host rejected");
            var fb = Commands.Download(new Options { Folder = temp, Platform = "facebook", Playlist = true }, "https://www.facebook.com/reel/123456789", temp);
            Assert(fb.Contains("--no-playlist") && !fb.Contains("--yes-playlist"), "Facebook downloads single video or Reel");
            Assert(Commands.ValidatePlatformUrl("https://m.instagram.com/p/1234", "instagram").Length > 0, "Instagram mobile link is accepted");
            Assert(Commands.ValidatePlatformUrl("https://www.youtube.com/watch?v=abc", "youtube").Length > 0, "YouTube link accepted");
            string twitchVod = "https://www.twitch.tv/videos/123456789?t=0h12m";
            Assert(Commands.ValidateTwitchVodUrl(twitchVod) == twitchVod, "Valid Twitch VOD URL with timestamp");
            Assert(Commands.ValidateTwitchVodUrl("https://www.twitch.tv/some_channel/video/123456789").Length > 0, "Alternate Twitch VOD URL");
            Assert(Commands.ValidateTwitchVodUrl("https://www.twitch.tv/some_channel/v/123456789").Length > 0, "Short channel VOD URL");
            Throws(() => Commands.ValidateTwitchVodUrl("https://www.twitch.tv/some_channel"), "Reject Twitch live channel URLs");
            Throws(() => Commands.ValidateTwitchVodUrl("https://clips.twitch.tv/example"), "Reject Twitch clips");
            Throws(() => Commands.ValidateTwitchVodUrl("https://twitch.tv.evil.test/videos/123456789"), "Reject non-Twitch domain");
            Throws(() => Commands.ValidateTwitchVodUrl("https://www.youtube.com/watch?v=123"), "Reject YouTube links in Twitch section");
            foreach (int height in new[] {720, 1080, 1440, 2160})
            {
                var opts = new Options { Folder = temp, Platform = "twitch", Playlist = true, Height = height };
                var command = Commands.Download(opts, twitchVod, temp);
                Assert(command.Contains("--no-playlist") && !command.Contains("--yes-playlist"), "Twitch VOD ignores playlist setting");
                Assert(command[command.IndexOf("--output") + 1].StartsWith("Twitch VODs/"), "Twitch VOD saved in its own folder");
                Assert(command[command.IndexOf("-f") + 1].Contains("height<=" + height), "Twitch VOD quality " + height);
            }
            foreach (int height in new[] {720, 1080, 1440, 2160})
            {
                var o = new Options { Folder = "C:\\Videos con espacios\\", Height = height };
                var a = Commands.Download(o, "https://example.com/video", "C:\\Motor con espacios");
                string selector = a[a.IndexOf("-f") + 1];
                Assert(selector == "bv*[height<=" + height + "]+ba/b[height<=" + height + "]", "Resolution ceiling " + height + " applies to both video branches");
                Assert(a.Contains("--merge-output-format") && a.Contains("--remux-video"), "MP4 merge and single-format remux " + height);
            }
            foreach (string rate in new[] {"320K", "256K", "192K", "128K"})
            {
                var a = Commands.Download(new Options { Folder = temp, Audio = true, Bitrate = rate }, "https://example.com/video", temp);
                Assert(a.Contains("--extract-audio") && a[a.IndexOf("--audio-format")+1] == "mp3" && a[a.IndexOf("--audio-quality")+1] == rate && !a.Contains("--merge-output-format"), "MP3 conversion " + rate);
            }
            var single = Commands.Download(new Options {Folder=temp, Playlist=false}, "https://example.com/a?x=1&list=2", temp);
            Assert(single.Contains("--no-playlist") && single[single.IndexOf("--playlist-items")+1] == "1", "Single item even for playlist-only URLs");
            Assert(single[single.IndexOf("--output")+1].StartsWith("Other/") && !single[single.IndexOf("--output")+1].Contains("playlist"), "Generic site uses Other folder for single download");
            var list = Commands.Download(new Options {Folder=temp, Playlist=true}, "https://example.com", temp);
            Assert(list.Contains("--yes-playlist") && !list.Contains("--playlist-items") && list[list.IndexOf("--output")+1].Contains("{:03d}") && list[list.IndexOf("--output")+1].StartsWith("Other/"), "Entire playlist, generic-site folder, ordered numbering");
            Assert(list.Contains("--no-abort-on-error") && !list.Contains("--ignore-errors"), "Unavailable videos continue without masking failed conversions");
            Assert(list.Contains("--no-overwrites") && list.Contains("--continue"), "Existing files protected and partial downloads resumable");
            Assert(list[list.Count-2] == "--", "URL separated from command options");
            Throws(() => Commands.Download(new Options {Height=999}, "https://example.com", temp), "Reject invalid height");
            string[] tricky = { "", "plain", "con espacios", "C:\\ruta con espacios\\", "quoted \"name\"", "& | ; $(x) `name`", "Melilla · vídeo 日本語", "tail\\\\", "\\\"" };
            var lines = new List<string>();
            int exit = await Runner.Run(Process.GetCurrentProcess().MainModule.FileName, new[] {"--fixture-echo"}.Concat(tricky), temp, s => { lock(lines) lines.Add(s); }, CancellationToken.None);
            var roundtrip = new JavaScriptSerializer().Deserialize<string[]>(lines.Last().TrimStart('\uFEFF'));
            Assert(exit == 0 && roundtrip.SequenceEqual(tricky), "Actual child-process argument round trip: spaces, quotes, Unicode, metacharacters and trailing slashes");
            byte[] payload = Encoding.UTF8.GetBytes("test data"); string file = Path.Combine(temp, "payload.bin"); File.WriteAllBytes(file, payload);
            string hash; using(var sha=SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(payload)).Replace("-", "").ToLowerInvariant();
            Assert(Installer.ParseChecksum(hash + "  yt-dlp.exe\n" + new string('0',64) + "  other.exe", "yt-dlp.exe") == hash, "Checksum manifest selects exact asset");
            Throws(() => Installer.ParseChecksum(hash + "  other.exe", "yt-dlp.exe"), "Missing checksum rejected");
            Installer.Verify(file, hash, CancellationToken.None); Assert(true, "Valid SHA256 accepted");
            Throws(() => Installer.Verify(file, new string('0',64), CancellationToken.None), "Corrupted download rejected");
            string zip = Path.Combine(temp, "fixture.zip"), extracted = Path.Combine(temp, "safe.exe");
            using(var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            { using(var writer=new StreamWriter(archive.CreateEntry("../../escape/ffmpeg.exe").Open())) writer.Write("fixture"); }
            Installer.ExtractOne(zip, "ffmpeg.exe", extracted, CancellationToken.None);
            Assert(File.ReadAllText(extracted) == "fixture" && !Directory.Exists(Path.Combine(temp,"escape")), "Archive paths cannot escape the fixed extraction target");
            var cts = new CancellationTokenSource(); var childReady = new TaskCompletionSource<int>();
            Task<int> wait = Runner.Run(Process.GetCurrentProcess().MainModule.FileName, new[] {"--fixture-parent"}, temp, s => { if(s.StartsWith("CHILD:")) childReady.TrySetResult(Int32.Parse(s.Substring(6))); }, cts.Token);
            Task readyOrTimeout = await Task.WhenAny(childReady.Task, Task.Delay(10000));
            if (readyOrTimeout != childReady.Task) { cts.Cancel(); throw new Exception("Child process readiness timeout"); }
            int childId = await childReady.Task; cts.Cancel(); bool cancelled = false;
            try { await wait; } catch(OperationCanceledException) { cancelled = true; }
            bool childStopped; try { using(var p=Process.GetProcessById(childId)) childStopped=p.HasExited; } catch(ArgumentException) { childStopped=true; }
            Assert(cancelled && childStopped, "Cancellation terminates the actual process tree, including descendants"); cts.Dispose();
        }
    }
}
