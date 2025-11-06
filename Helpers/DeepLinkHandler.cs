using CATERINGMANAGEMENT.View.Windows;
using System.Windows;

namespace CATERINGMANAGEMENT.Helpers
{
    public static class DeepLinkHandler
    {
        /// <summary>
        /// Handles custom protocol deep links. Returns true if a window was opened.
        /// Also enforces optional issued/exp query params appended by our redirect_to.
        /// </summary>
        public static bool Handle(Uri uri)
        {
            if (uri == null) return false;

            // validate optional expiry (issued/exp)
            var all = Merge(ParseKvp(uri.Fragment, isFragment: true), ParseKvp(uri.Query, isFragment: false));
            if (all.TryGetValue("exp", out var expStr) && long.TryParse(expStr, out var expUnix))
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now > expUnix)
                {
                    MessageBox.Show("This link has expired. Please request a new one.", "Link expired", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Only handle our custom scheme hosts
            if (uri.Host.Equals("reset-password", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = all; // already merged

                tokens.TryGetValue("type", out var type);
                tokens.TryGetValue("access_token", out var accessToken);
                tokens.TryGetValue("refresh_token", out var refreshToken);

                if (string.Equals(type, "recovery", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(accessToken))
                {
                    var opened = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var win = new ResetPasswordWindow(accessToken!, refreshToken);

                        // Make ResetPassword the main window and show it
                        Application.Current.MainWindow = win;
                        win.Show();
                        win.Activate();
                        win.Focus();
                        opened = true;
                    });
                    return opened;
                }
            }

            return false;
        }

        private static Dictionary<string, string> ParseKvp(string input, bool isFragment)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(input)) return dict;

            // Trim leading '?' or '#'
            if (isFragment && input.StartsWith("#")) input = input[1..];
            if (!isFragment && input.StartsWith("?")) input = input[1..];

            var pairs = input.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(kv[0]);
                var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;
                dict[key] = value;
            }
            return dict;
        }

        private static Dictionary<string, string> Merge(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in a) result[kv.Key] = kv.Value;
            foreach (var kv in b) result[kv.Key] = kv.Value;
            return result;
        }
    }
}
