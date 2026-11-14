using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace HKDownloader
{
    // Updates come only from the official repository's latest published GitHub Release.
    // A strong SHA-256 checksum is required; an unverified binary is never installed.
    public static class AppUpdater
    {
        const string LatestRelease = "https://api.github.com/repos/HKBilel/HKDownloader/releases/latest";
        const string Executable = "HKDownloader.exe";

        public sealed class Release
        {
            public Version Number;
            public string Tag, Notes, DownloadUrl, Sha256;
        }

        public static Version ParseVersion(string tag)
        {
            Match m = Regex.Match(tag ?? "", @"^v?(\d+)\.(\d+)(?:\.(\d+))?$", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return new Version(Int32.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                Int32.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                m.Groups[3].Success ? Int32.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0);
        }
        // Only published versions newer than this installed version are offered.
        public static bool Newer(Version version)
        {
            return version != null && version > Assembly.GetExecutingAssembly().GetName().Version;
        }
        public static string SafeHash(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            string text = value.Trim();
            if (text.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) text = text.Substring(7);
            return Regex.IsMatch(text, "^[a-f0-9]{64}$", RegexOptions.IgnoreCase) ? text.ToLowerInvariant() : null;
        }
        static Dictionary<string, object> AsObject(object obj) { return obj as Dictionary<string, object>; }
        static string Get(Dictionary<string, object> obj, string field)
        {
            object value;
            return obj != null && obj.TryGetValue(field, out value) && value != null ? Convert.ToString(value) : "";
        }
        static HttpClient Client()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HKDownloader/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }
        static bool TrustedUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) || uri.Scheme != "https") return false;
            // GitHub release assets can redirect to objects.githubusercontent.com, but the initial asset URL
            // must be GitHub's exact release-asset path for our repository.
            return uri.DnsSafeHost.Equals("github.com", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.StartsWith("/HKBilel/HKDownloader/releases/download/", StringComparison.Ordinal);
        }
        public static async Task<Release> CheckAsync()
        {
            using (var http = Client())
            using (var response = await http.GetAsync(LatestRelease))
            {
                if (response.StatusCode == HttpStatusCode.NotFound) return null; // No release published yet.
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync();
                var json = new JavaScriptSerializer { MaxJsonLength = 2000000 };
                var release = AsObject(json.DeserializeObject(body));
                if (release == null) throw new IOException("Invalid GitHub release metadata.");
                string tag = Get(release, "tag_name");
                Version version = ParseVersion(tag);
                if (version == null) return null;
                var assets = release["assets"] as IEnumerable;
                Dictionary<string, object> binary = null, sums = null;
                if (assets != null) foreach (object raw in assets)
                {
                    var asset = AsObject(raw);
                    if (Get(asset, "name").Equals(Executable, StringComparison.OrdinalIgnoreCase)) binary = asset;
                    if (Get(asset, "name").Equals(Executable + ".sha256", StringComparison.OrdinalIgnoreCase)) sums = asset;
                }
                if (binary == null || !TrustedUrl(Get(binary, "browser_download_url"))) return null;
                string sha = SafeHash(Get(binary, "digest"));
                // A release checksum is required when GitHub does not expose an asset digest.
                if (sha == null && sums != null && TrustedUrl(Get(sums, "browser_download_url")))
                {
                    using (var checksumResponse = await http.GetAsync(Get(sums, "browser_download_url")))
                    {
                        checksumResponse.EnsureSuccessStatusCode();
                        string checksumText = await checksumResponse.Content.ReadAsStringAsync();
                        sha = SafeHash(Regex.Match(checksumText, "^[a-fA-F0-9]{64}").Value);
                    }
                }
                if (sha == null) return null; // Fail closed when no integrity evidence exists.
                return new Release { Number = version, Tag = tag, Notes = Get(release, "body"),
                    DownloadUrl = Get(binary, "browser_download_url"), Sha256 = sha };
            }
        }
        public static async Task<string> DownloadVerifiedAsync(Release release)
        {
            string stage = Path.Combine(Path.GetTempPath(), "HKDownloader-Update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stage);
            string destination = Path.Combine(stage, Executable);
            try
            {
                using (var http = Client())
                using (var response = await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var source = await response.Content.ReadAsStreamAsync())
                    using (var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[65536]; long total = 0; int bytes;
                        while ((bytes = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            total += bytes;
                            if (total > 150L * 1024 * 1024) throw new IOException("Update is larger than the allowed limit.");
                            await target.WriteAsync(buffer, 0, bytes);
                        }
                        if (total == 0) throw new IOException("Empty update downloaded.");
                    }
                }
                string actual;
                using (var file = File.OpenRead(destination))
                using (var sha = SHA256.Create())
                    actual = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                if (!String.Equals(release.Sha256, actual, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Update SHA-256 verification failed.");
                return destination;
            }
            catch
            {
                try { Directory.Delete(stage, true); } catch { }
                throw;
            }
        }
        // The active executable cannot be replaced in place. A one-shot helper starts after the app exits.
        public static void InstallAfterExit(string downloadedFile)
        {
            string program = Assembly.GetExecutingAssembly().Location;
            if (!Path.GetFileName(program).Equals(Executable, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Automatic updates require the installed file to be named " + Executable + ".");
            string helper = Path.Combine(Path.GetDirectoryName(downloadedFile), "Apply-Update.ps1");
            const string script = @"param([int]$AppProcessId, [string]$Source, [string]$Target)
$ErrorActionPreference = 'Stop'
try { Wait-Process -Id $AppProcessId -Timeout 75 -ErrorAction SilentlyContinue } catch {}
$backup = $Target + '.previous'
for ($attempt = 0; $attempt -lt 40; $attempt++) {
    try {
        if (Test-Path -LiteralPath $Target) { Copy-Item -LiteralPath $Target -Destination $backup -Force -ErrorAction Stop }
        Copy-Item -LiteralPath $Source -Destination $Target -Force -ErrorAction Stop
        Start-Process -FilePath $Target -ErrorAction Stop
        exit 0
    } catch {
        if (Test-Path -LiteralPath $backup) {
            try { Copy-Item -LiteralPath $backup -Destination $Target -Force -ErrorAction Stop } catch {}
        }
        Start-Sleep -Seconds 1
    }
}
Write-Output 'Could not replace HKDownloader. Install manually from GitHub Releases.'
exit 1
";
            File.WriteAllText(helper, script, new UTF8Encoding(true));
            var process = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "
                + Commands.Quote(helper) + " -AppProcessId " + Process.GetCurrentProcess().Id + " -Source "
                + Commands.Quote(downloadedFile) + " -Target " + Commands.Quote(program))
            { UseShellExecute = false, CreateNoWindow = true };
            Process.Start(process);
        }
    }
}