using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CATERINGMANAGEMENT.Helpers
{
    public static class UriProtocolRegistrar
    {
        public enum Scope { CurrentUser, LocalMachine }

        // Back-compat overload used by App.xaml.cs
        public static void EnsureRegistered(string protocol, string applicationPath)
            => EnsureRegistered(protocol, applicationPath, Scope.CurrentUser, forceUpdate: true);

        public static void EnsureRegistered(string protocol, string applicationPath, bool forceUpdate)
            => EnsureRegistered(protocol, applicationPath, Scope.CurrentUser, forceUpdate);

        public static void EnsureRegistered(string protocol, string applicationPath, Scope scope, bool forceUpdate = true)
        {
            try
            {
                if (!IsValidProtocol(protocol) || string.IsNullOrWhiteSpace(applicationPath))
                    return;

                protocol = protocol.Trim().ToLowerInvariant();

                var exePath = File.Exists(applicationPath)
                    ? applicationPath
                    : (Process.GetCurrentProcess().MainModule?.FileName ?? applicationPath);

                if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                    return;

                var desiredCommand = $"\"{exePath}\" \"%1\"";

                using var root = OpenRoot(scope);
                using var key = root.CreateSubKey($@"Software\Classes\{protocol}");
                if (key == null) return;

                // Base scheme keys (idempotent)
                key.SetValue(string.Empty, $"URL:{protocol} Protocol");
                key.SetValue("URL Protocol", string.Empty);

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                    defaultIcon?.SetValue(string.Empty, $"\"{exePath}\",1");

                using var commandKey = key.CreateSubKey(@"shell\open\command");
                var currentCommand = commandKey?.GetValue(string.Empty) as string;

                if (forceUpdate || string.IsNullOrWhiteSpace(currentCommand) ||
                    !string.Equals(currentCommand, desiredCommand, StringComparison.OrdinalIgnoreCase))
                {
                    commandKey?.SetValue(string.Empty, desiredCommand);
                    Debug.WriteLine($"[URI Registrar] Set command to: {desiredCommand}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[URI Registrar] Failed to register/update protocol: {ex.Message}");
            }
        }

        public static void Unregister(string protocol, Scope scope = Scope.CurrentUser)
        {
            try
            {
                if (!IsValidProtocol(protocol)) return;
                using var root = OpenRoot(scope);
                root.DeleteSubKeyTree($@"Software\Classes\{protocol}", throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[URI Registrar] Failed to unregister protocol: {ex.Message}");
            }
        }

        private static RegistryKey OpenRoot(Scope scope)
        {
            var hive = scope == Scope.LocalMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
            return RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        }

        private static bool IsValidProtocol(string p) =>
            !string.IsNullOrWhiteSpace(p) && Regex.IsMatch(p, "^[a-z][a-z0-9+.-]*$", RegexOptions.IgnoreCase);
    }
}
