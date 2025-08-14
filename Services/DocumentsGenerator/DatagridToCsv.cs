using Microsoft.Win32;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace CATERINGMANAGEMENT.Services.DocumentsGenerator
{
    public static class DatagridToCsv
    {
        public static void ExportToCsv(IEnumerable dataSource, params string[] skipProperties)
        {
            if (dataSource == null)
                return;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "Equipments.csv"
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

            // Show success message
            MessageBox.Show("CSV file has been saved successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
