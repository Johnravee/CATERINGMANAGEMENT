using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Mailer;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;

public class AssignWorkersViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Reservation> Reservations { get; } = new();
    public ObservableCollection<Worker> Workers { get; } = new();
    public ObservableCollection<Worker> AssignedWorkers { get; } = new();

    private readonly CollectionViewSource _filteredWorkers = new();
    public ICollectionView FilteredWorkers => _filteredWorkers.View;

    private Reservation? _selectedReservation;
    public Reservation? SelectedReservation
    {
        get => _selectedReservation;
        set { _selectedReservation = value; OnPropertyChanged(); }
    }

    private string? _notes;
    public string? Notes
    {
        get => _notes;
        set { _notes = value; OnPropertyChanged(); }
    }

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilteredWorkers.Refresh();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand AssignWorkerCommand { get; }
    public ICommand RemoveAssignedWorkerCommand { get; }
    public ICommand BatchAssignCommand { get; }
    public ICommand CancelCommand { get; }

    public AssignWorkersViewModel()
    {
        AssignWorkerCommand = new RelayCommand<Worker>(w => ToggleAssign(w));
        RemoveAssignedWorkerCommand = new RelayCommand<Worker>(w => RemoveAssignedWorker(w));
        BatchAssignCommand = new RelayCommand(async () => await BatchAssignWorkers());
        CancelCommand = new RelayCommand(() => CloseWindow());

        _filteredWorkers.Source = Workers;
        _filteredWorkers.Filter += ApplyFilter;

        _ = LoadData();
    }

    private void ApplyFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is Worker worker)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                e.Accepted = true;
            else
            {
                string query = SearchText.ToLower();
                e.Accepted = (worker.Name?.ToLower().Contains(query) ?? false)
                          || (worker.Role?.ToLower().Contains(query) ?? false)
                          || (worker.Email?.ToLower().Contains(query) ?? false)
                          || (worker.Contact?.ToLower().Contains(query) ?? false);
            }
        }
    }

    private void ToggleAssign(Worker worker)
    {
        if (worker == null) return;

        if (AssignedWorkers.Contains(worker))
            AssignedWorkers.Remove(worker);
        else
            AssignedWorkers.Add(worker);
    }

    private void RemoveAssignedWorker(Worker worker)
    {
        if (worker == null) return;
        AssignedWorkers.Remove(worker);
    }

    private async Task LoadData()
    {
        try
        {
            IsLoading = true;

            var client = await SupabaseService.GetClientAsync();

            var reservations = await client
                .From<Reservation>()
                .Select("*, package:package_id(*)")
                .Where(r => r.Status == "done")
                .Get();

            Reservations.Clear();
            foreach (var r in reservations.Models) Reservations.Add(r);

            var workers = await client.From<Worker>().Get();
            Workers.Clear();
            foreach (var w in workers.Models) Workers.Add(w);

            FilteredWorkers.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BatchAssignWorkers()
    {
        if (SelectedReservation == null || AssignedWorkers.Count == 0)
        {
            MessageBox.Show("Please select a reservation and at least one worker.");
            return;
        }

        try
        {
            IsLoading = true;

            var client = await SupabaseService.GetClientAsync();

            var schedules = AssignedWorkers.Select(w => new Scheduling
            {
                ReservationId = (int)SelectedReservation.Id,
                WorkerId = w.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await client.From<Scheduling>().Insert(schedules);

            var mailer = new AssignWorkerMailer(new EmailService());
            foreach (var worker in AssignedWorkers)
            {
                bool emailSent = mailer.SendWorkerScheduleEmail(
                    worker.Email ?? "",
                    worker.Name ?? "Staff",
                    worker.Role ?? "Staff",
                    SelectedReservation.Package?.Name ?? "Event",
                    SelectedReservation.EventDate.ToString("MMMM dd, yyyy"),
                    SelectedReservation.Venue ?? "Venue"
                );
                if (!emailSent)
                {
                    MessageBox.Show($"Failed to send email to {worker.Name} ({worker.Email})");
                }
            }

            MessageBox.Show("Workers successfully assigned!");
            CloseWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error assigning workers:\n{ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CloseWindow()
    {
        var win = Application.Current.Windows.OfType<AssignWorker>().FirstOrDefault();
        win?.Close();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
