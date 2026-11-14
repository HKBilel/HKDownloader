using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Threading;

namespace HKDownloader
{
    // Local tamper detection for tools downloaded from verified HTTPS release assets.
    // This is not a digital signature: another program with write access to BOTH
    // tools and the local manifest can replace them together. Keep tools in a
    // user-controlled directory with appropriate Windows file permissions.
    public static class ToolIntegrity
    {
        static readonly string[] Managed = { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" };
        const string ManifestName = "integrity.json";

        static bool IsManaged(string name)
        {
            return Managed.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
        static string ToolPath(string directory, string name)
        {
            if (!IsManaged(name)) throw new ArgumentException("Unmanaged tool name.", "name");
            return Path.Combine(directory, name);
        }
        static Dictionary<string, string> ReadManifest(string directory)
        {
            string path = Path.Combine(directory, ManifestName);
            if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (new FileInfo(path).Length > 16 * 1024) throw new IOException("Integrity manifest too large.");
                var parsed = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(path, Encoding.UTF8));
                if (parsed == null) throw new IOException("Invalid integrity manifest.");
                return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                if (e is OutOfMemoryException || e is StackOverflowException || e is ThreadAbortException) throw;
                // Fail closed: missing, modified or malformed manifests are never trusted.
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }
        public static string Sha256(string path, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Managed tools must not be symbolic links or reparse points.");
            var file = new FileInfo(path);
            if (file.Length == 0 || file.Length > 1024L * 1024 * 1024)
                throw new IOException("Managed tool size is invalid.");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create())
            {
                byte[] buffer = new byte[128 * 1024];
                int size;
                while ((size = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    sha.TransformBlock(buffer, 0, size, buffer, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
        public static bool Verify(string directory, params string[] names)
        {
            return Verify(directory, CancellationToken.None, names);
        }
        public static bool Verify(string directory, CancellationToken token, params string[] names)
        {
            try
            {
                var expected = ReadManifest(directory);
                foreach (string name in names)
                {
                    string fingerprint;
                    if (!expected.TryGetValue(name, out fingerprint) ||
                        !Regex.IsMatch(fingerprint ?? "", "^[a-f0-9]{64}$", RegexOptions.IgnoreCase) ||
                        !String.Equals(Sha256(ToolPath(directory, name), token),
                                       fingerprint, StringComparison.OrdinalIgnoreCase)) return false;
                }
                return true;
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException || e is OutOfMemoryException ||
                    e is StackOverflowException || e is ThreadAbortException) throw;
                return false; // An unreadable executable is not safe to launch.
            }
        }
        public static bool Ready(string directory) { return Verify(directory, Managed); }
        public static void AssertExecutable(string exe, CancellationToken token)
        {
            string name = Path.GetFileName(exe);
            if (!IsManaged(name)) return; // Self-tests also run HKDownloader.exe through Runner.
            token.ThrowIfCancellationRequested();
            if (!Verify(Path.GetDirectoryName(exe), token, name))
                throw new IOException(Loc.T("toolIntegrityFail") + " " + name);
        }
        public static void Record(string directory, CancellationToken token, params string[] names)
        {
            Directory.CreateDirectory(directory);
            var fingerprints = ReadManifest(directory);
            foreach (string name in names)
                fingerprints[name] = Sha256(ToolPath(directory, name), token);
            token.ThrowIfCancellationRequested();
            string manifest = Path.Combine(directory, ManifestName);
            string stage = Path.Combine(directory, ".tool-integrity-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(stage, new JavaScriptSerializer().Serialize(fingerprints), new UTF8Encoding(false));
                if (File.Exists(manifest)) File.Replace(stage, manifest, null);
                else File.Move(stage, manifest);
            }
            finally { if (File.Exists(stage)) File.Delete(stage); }
        }
    }
}
