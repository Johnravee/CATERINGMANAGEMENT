using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace CATERINGMANAGEMENT.Helpers
{
    public static class UriProtocolRegistrar
    {
        public static void EnsureRegistered(string protocol, string applicationPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(protocol) || string.IsNullOrWhiteSpace(applicationPath))
                    return;

                protocol = protocol.Trim().ToLowerInvariant();

                using var root = Registry.CurrentUser.OpenSubKey($"Software\\Classes\\{protocol}");
                if (root != null)
                {
                    return; // already registered for current user
                }

                using var key = Registry.CurrentUser.CreateSubKey($"Software\\Classes\\{protocol}");
                if (key == null) return;

                key.SetValue(string.Empty, $"URL:{protocol} Protocol");
                key.SetValue("URL Protocol", string.Empty);

                using var defaultIcon = key.CreateSubKey("DefaultIcon");
                defaultIcon?.SetValue(string.Empty, $"\"{applicationPath}\",1");

                using var commandKey = key.CreateSubKey("shell\\open\\command");
                var exePath = applicationPath;
                if (!File.Exists(exePath))
                {
                    exePath = Process.GetCurrentProcess().MainModule?.FileName ?? applicationPath;
                }
                commandKey?.SetValue(string.Empty, $"\"{exePath}\" \"%1\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register URI protocol: {ex.Message}");
            }
        }
    }
}
