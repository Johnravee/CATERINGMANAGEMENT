using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace CATERINGMANAGEMENT.DocumentsGenerator
{
    public static class DatagridToCsv
    {
        public static void ExportToCsv(IEnumerable dataSource, string filename  ,params string[] skipProperties)
        {
            if (dataSource == null || !dataSource.Cast<object>().Any())
            {
                MessageBox.Show("No data available to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = filename
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            var sb = new StringBuilder();

            // Get all public properties of the first item
            var firstItem = dataSource.Cast<object>().FirstOrDefault();
            if (firstItem == null)
                return;

            var properties = firstItem.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !skipProperties.Contains(p.Name))
                .ToList();

            // Write header in uppercase
            sb.AppendLine(string.Join(",", properties.Select(p => p.Name.ToUpper())));

            // Write rows
            foreach (var item in dataSource)
            {
                var row = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? "";
                    return value.Replace(",", ";");
                });
                sb.AppendLine(string.Join(",", row));
            }

            // Save file
            File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
            Process.Start(new ProcessStartInfo
            {
                FileName = saveFileDialog.FileName,
                UseShellExecute = true
            });    
        }
    }
}
