using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HKDownloader
{
    // Non-destructive user-selected ending trim; it cannot detect logos/end cards.
    public static class MediaCleanup
    {
        public static string NewTrimmedPath(string original)
        {
            string folder = Path.GetDirectoryName(original);
            string basename = Path.GetFileNameWithoutExtension(original);
            string extension = ".mp4";
            string candidate = Path.Combine(folder, basename + " (ending trimmed)" + extension);
            int n = 2;
            while (File.Exists(candidate))
                candidate = Path.Combine(folder, basename + " (ending trimmed " + (n++).ToString(CultureInfo.InvariantCulture) + ")" + extension);
            return candidate;
        }

        public static string[] TrimArgs(string input, string output, double newDuration)
        {
            if (!(newDuration > 0) || Double.IsNaN(newDuration) || Double.IsInfinity(newDuration))
                throw new ArgumentException("The remaining video length must be positive.");
            return new[] {
                "-hide_banner", "-loglevel", "error", "-nostdin", "-n",
                "-i", input, "-t", newDuration.ToString("0.###", CultureInfo.InvariantCulture),
                "-map", "0:v:0", "-map", "0:a?", "-c:v", "libx264", "-preset", "veryfast",
                "-crf", "19", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", output
            };
        }

        // Returns an additional MP4 while preserving the downloaded original.
        public static async Task<string> TrimAsync(string input, int cutSeconds, string tools, string working,
            CancellationToken token, Action<string> report)
        {
            if (!new[] { 1, 2, 3, 5 }.Contains(cutSeconds))
                throw new ArgumentException("Unsupported trim duration.");
            if (!File.Exists(input)) throw new FileNotFoundException("Downloaded media is missing.", input);
            var probeLines = new List<string>();
            var probeArgs = new[] { "-v", "error", "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1", input };
            int probeCode = await Runner.Run(Path.Combine(tools, "ffprobe.exe"), probeArgs, working,
                line => { lock(probeLines) probeLines.Add(line); }, token);
            double duration;
            if (probeCode != 0 || !probeLines.Any(line =>
                Double.TryParse(line.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out duration)))
                throw new IOException("Unable to determine video duration.");
            if (!Double.TryParse(probeLines.First(line =>
                Double.TryParse(line.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out duration)).Trim(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out duration)
                || duration - cutSeconds < 1.0)
                throw new IOException("Video too short to trim without losing the main content.");

            // Reserve nothing in advance; FFmpeg refuses overwrites and we never alter the original.
            string output = NewTrimmedPath(input);
            report("Creating trimmed copy: " + Path.GetFileName(output));
            bool complete = false;
            try
            {
                int code = await Runner.Run(Path.Combine(tools, "ffmpeg.exe"),
                    TrimArgs(input, output, duration - cutSeconds), working, line => report(line), token);
                if (code != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
                    throw new IOException("FFmpeg failed to create the trimmed copy.");
                complete = true;
                return output;
            }
            finally
            {
                // Cancellation and FFmpeg errors never leave a misleading partial 'trimmed' file.
                if (!complete) { try { if (File.Exists(output)) File.Delete(output); } catch { } }
            }
        }
    }
}
