// Converts the selected HKDownloader SVG into Windows icon assets using WPF already installed
// with .NET Framework. No online services, image packages, or downloaded binaries required.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class LogoGenerator
{
    private sealed class Shape
    {
        public Geometry Geometry;
        public Brush Brush;
    }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3 && args.Length != 5)
                throw new ArgumentException("Usage: LogoGenerator <input.svg> <output.png> <output.ico> [backgroundHex foregroundHex]");
            Generate(args[0], args[1], args[2],
                args.Length == 5 ? args[3] : null,
                args.Length == 5 ? args[4] : null);
            Console.WriteLine("Rendered HKDownloader logo: PNG + multi-size ICO");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.ToString());
            return 1;
        }
    }

    private static void Generate(string sourceSvg, string outputPng, string outputIco,
        string backgroundHex, string foregroundHex)
    {
        var document = new XmlDocument();
        document.XmlResolver = null;
        var xmlSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using (var reader = XmlReader.Create(sourceSvg, xmlSettings)) document.Load(reader);

        // Read the actual colors from the artist's SVG, rather than using hardcoded
        // old blue/white paints. Respect Illustrator's hidden editing groups.
        var colors = new Dictionary<string, string>(StringComparer.Ordinal);
        var hidden = new HashSet<string>(StringComparer.Ordinal);
        foreach (XmlNode style in document.SelectNodes("//*[local-name()='style']"))
        {
            foreach (Match rule in Regex.Matches(style.InnerText, @"\.([a-zA-Z0-9_-]+)\s*\{([^}]*)\}"))
            {
                string name = rule.Groups[1].Value;
                string declaration = rule.Groups[2].Value;
                Match fill = Regex.Match(declaration, @"\bfill\s*:\s*(#[0-9a-fA-F]{3,8})\s*;?", RegexOptions.IgnoreCase);
                if (fill.Success) colors[name] = fill.Groups[1].Value;
                if (Regex.IsMatch(declaration, @"\bdisplay\s*:\s*none\b", RegexOptions.IgnoreCase))
                    hidden.Add(name);
            }
        }

        // Optional platform variation: modify only CSS colors, never the vector paths.
        // The three-argument build keeps the SVG's original colors, including the separate yellow app icon.
        if (backgroundHex != null || foregroundHex != null)
        {
            if (backgroundHex == null || foregroundHex == null ||
                !Regex.IsMatch(backgroundHex, "^#[0-9a-fA-F]{6}$") ||
                !Regex.IsMatch(foregroundHex, "^#[0-9a-fA-F]{6}$"))
                throw new ArgumentException("Platform colors must be #RRGGBB.");
            if (!colors.ContainsKey("st0") || !colors.ContainsKey("st3"))
                throw new InvalidDataException("Source SVG no longer has the expected colored layers.");
            colors["st0"] = backgroundHex;
            colors["st3"] = foregroundHex;
        }

        var shapes = new List<Shape>();
        foreach (XmlNode node in document.SelectNodes("//*[local-name()='rect' or local-name()='path']"))
        {
            bool concealed = false;
            for (XmlNode ancestor = node; ancestor != null; ancestor = ancestor.ParentNode)
            {
                XmlAttribute marker = ancestor.Attributes == null ? null : ancestor.Attributes["class"];
                if (marker != null && hidden.Contains(marker.Value))
                {
                    concealed = true;
                    break;
                }
            }
            if (concealed) continue;
            var cssClass = node.Attributes["class"];
            string paint;
            if (cssClass == null || !colors.TryGetValue(cssClass.Value, out paint)) continue;

            Geometry geometry;
            if (node.LocalName == "path")
            {
                var pathData = node.Attributes["d"];
                if (pathData == null) continue;
                geometry = Geometry.Parse(pathData.Value);
            }
            else
            {
                Func<string, double, double> attr = (key, fallback) =>
                {
                    XmlAttribute value = node.Attributes[key];
                    return value == null ? fallback : Double.Parse(value.Value, CultureInfo.InvariantCulture);
                };
                var rectangle = new System.Windows.Rect(
                    attr("x", 0), attr("y", 0), attr("width", 0), attr("height", 0));
                geometry = new RectangleGeometry(rectangle, attr("rx", 0), attr("ry", attr("rx", 0)));
            }
            geometry.Freeze();
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(paint));
            brush.Freeze();
            shapes.Add(new Shape { Geometry = geometry, Brush = brush });
        }
        if (shapes.Count < 4)
            throw new InvalidDataException("Expected at least four visible SVG shapes; got " + shapes.Count);

        int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
        var frames = new List<byte[]>();
        foreach (int size in sizes)
        {
            var drawing = new DrawingVisual();
            using (var dc = drawing.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(size / 1024.0, size / 1024.0));
                foreach (Shape shape in shapes) dc.DrawGeometry(shape.Brush, null, shape.Geometry);
                dc.Pop();
            }
            var bitmap = new RenderTargetBitmap(size, size, 96.0, 96.0, PixelFormats.Pbgra32);
            bitmap.Render(drawing);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var png = new MemoryStream())
            {
                encoder.Save(png);
                byte[] bytes = png.ToArray();
                frames.Add(bytes);
                if (size == 256) File.WriteAllBytes(outputPng, bytes);
            }
        }

        // ICO with a PNG image for every resolution: Vista/Windows 10/11 compatible.
        // These are real 16/24/32/48/64/128/256 pixel frames, not just a scaled 256px file.
        using (var output = new BinaryWriter(File.Create(outputIco)))
        {
            output.Write((ushort)0); // reserved
            output.Write((ushort)1); // icon
            output.Write((ushort)sizes.Length);
            uint offset = (uint)(6 + 16 * sizes.Length);
            for (int i = 0; i < sizes.Length; ++i)
            {
                output.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                output.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                output.Write((byte)0); // palette
                output.Write((byte)0); // reserved
                output.Write((ushort)1); // planes
                output.Write((ushort)32); // bit depth
                output.Write((uint)frames[i].Length);
                output.Write(offset);
                offset += (uint)frames[i].Length;
            }
            foreach (byte[] frame in frames) output.Write(frame);
        }
    }
}
