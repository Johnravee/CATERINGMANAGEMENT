using System.Windows.Controls;
using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.ViewModels;


namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class EquipmentsInventory : Page
    {
        private readonly EquipmentsViewModel _viewModel;

        public EquipmentsInventory()
        {
            InitializeComponent();
            _viewModel = new EquipmentsViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) =>
            {
                await _viewModel.LoadEquipments();
            };
        }

        private void ExportAsCsv(object sender, System.Windows.RoutedEventArgs e)
        {
            DatagridToCsv.ExportToCsv(
               EquipmentDataGrid.ItemsSource,
               "EquipmentsInventory.csv",
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
                EquipmentDataGrid.ItemsSource,
                "EquipmentsInventory.pdf",
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
