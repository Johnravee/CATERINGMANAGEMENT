using System;
using System.IO;
using System.Windows;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    internal static class DocumentResourceHelper
    {
        // Returns a filesystem path to a logo image. If a temporary file was created (extracted from resources or placeholder),
        // the path will point to that temp file and outTempPath will contain its path (caller should delete it). If the file
        // exists on disk in output folder, outTempPath will be null and returned path points directly to that file.
        public static string? GetLogoPath(out string? outTempPath)
        {
            outTempPath = null;
            var candidates = new[]
            {
                "Assets/images/documentlogo.png",
                "Assets/images/applogo.png",
                "Assets/images/logoicon.ico",
                "Assets/images/oshdylogo.jpg"
            };

            // 1) Try pack URI resources first
            foreach (var c in candidates)
            {
                try
                {
                    var packUri = new Uri($"pack://application:,,,/{c}", UriKind.Absolute);
                    var res = Application.GetResourceStream(packUri);
                    if (res != null)
                    {
                        var ext = Path.GetExtension(c);
                        var tempFile = Path.Combine(Path.GetTempPath(), $"catering_asset_{Guid.NewGuid()}{ext}");
                        using (var fs = File.Create(tempFile))
                        {
                            res.Stream.CopyTo(fs);
                        }
                        outTempPath = tempFile;
                        return tempFile;
                    }
                }
                catch
                {
                    // ignore and continue
                }
            }

            // 2) Check AppContext.BaseDirectory
            foreach (var c in candidates)
            {
                try
                {
                    var combined = Path.Combine(AppContext.BaseDirectory, c.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(combined)) return combined;
                }
                catch { }
            }

            // 3) Check current working directory
            foreach (var c in candidates)
            {
                try
                {
                    if (File.Exists(c)) return Path.GetFullPath(c);
                }
                catch { }
            }

            // 4) fallback tiny placeholder (transparent PNG)
            try
            {
                const string b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAAWgmWQ0AAAAASUVORK5CYII=";
                var bytes = Convert.FromBase64String(b64);
                var tempFile = Path.Combine(Path.GetTempPath(), $"catering_placeholder_{Guid.NewGuid()}.png");
                File.WriteAllBytes(tempFile, bytes);
                outTempPath = tempFile;
                return tempFile;
            }
            catch
            {
                outTempPath = null;
                return null;
            }
        }
    }
}
