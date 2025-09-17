using System.Windows.Controls;
using CATERINGMANAGEMENT.DocumentsGenerator;
using CATERINGMANAGEMENT.ViewModels;


namespace CATERINGMANAGEMENT.View.Pages
{
    public partial class EquipmentsInventory : Page
    {
        private readonly EquipmentViewModel _viewModel;

        public EquipmentsInventory()
        {
            InitializeComponent();
            //AuthGuard.RequireAuthentication(this);
            _viewModel = new EquipmentViewModel();
            DataContext = _viewModel;

            Loaded += async (_, __) =>
            {
                await _viewModel.LoadItems();
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
