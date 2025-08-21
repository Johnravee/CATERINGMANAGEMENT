using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class KitchenInventory : Page
    {
        private readonly KitchenViewModel _viewModel;

        public KitchenInventory()
        {
            InitializeComponent();
            _viewModel = new KitchenViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) => { await _viewModel.LoadItems(); };
        }

        private void ExportAsCsv(object sender, System.Windows.RoutedEventArgs e)
        {
            DatagridToCsv.ExportToCsv(
                KitchenDataGrid.ItemsSource,
                "KitchenInventory.csv",
                "Id",
                "BaseUrl",
                "RequestClientOptions",
                "TableName",
                "PrimaryKey",
                "UpdatedAt",
                "CreatedAt"
            );
        }

        [Obsolete]
        private void ExportAsPDF(object sender, System.Windows.RoutedEventArgs e)
        {
            DataGridToPdf.DataGridToPDF(
                KitchenDataGrid.ItemsSource,
                "KitchenInventory.pdf",
                "Id",
                "BaseUrl",
                "RequestClientOptions",
                "TableName",
                "PrimaryKey",
                "UpdatedAt",
                "CreatedAt"
            );
        }

    
    }
}
