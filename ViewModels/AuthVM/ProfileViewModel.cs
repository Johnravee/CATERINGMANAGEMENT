using CATERINGMANAGEMENT.Helpers;
using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace CATERINGMANAGEMENT.ViewModels.AuthVM
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Profile> _profiles = new();
        public ObservableCollection<Profile> Profiles
        {
            get => _profiles;
            set { _profiles = value; OnPropertyChanged(); }
        }


     

  


        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public async Task LoadProfiles()
        {
            IsLoading = true;
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var response = await client
                    .From<Profile>()
                    .Select("*")
                    .Get();

                Profiles.Clear();
                if (response.Models != null)
                {
                    foreach (var profile in response.Models)
                    {
                        Profiles.Add(profile);
                        Debug.WriteLine($"✅ Loaded profile: {profile.FullName} ({profile.Email})");
                    }
                }
                else
                {
                    Debug.WriteLine("⚠ No profiles returned from Supabase.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profiles:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                Debug.WriteLine($"❌ Error loading profiles: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public ICommand SaveCommand { get; }

        public ProfileViewModel()
        {
            SaveCommand = new RelayCommand(async () => await InsertProfileAsync());
        }

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string Address { get => _address; set { _address = value; OnPropertyChanged(); } }
        public string ContactNumber { get => _contactNumber; set { _contactNumber = value; OnPropertyChanged(); } }

        private string _name = string.Empty;
        private string _address = string.Empty;
        private string _contactNumber = string.Empty;


        public async Task<bool> InsertProfileAsync()
        {
            try
            {
                var client = await SupabaseService.GetClientAsync();

                var newProfile = new Profile
                {
                    FullName = Name,
                    Email = SessionService.CurrentUser?.Email,
                    Address = Address,
                    ContactNumber = ContactNumber,
                    FcmToken = null,
                    AuthId = SessionService.CurrentUser?.Id
                };

                var response = await client
                    .From<Profile>()
                    .Insert(newProfile);

                
                if (response.Models != null && response.Models.Count > 0)
                {
                    var insertedProfile = response.Models[0];
                    Debug.WriteLine($"✅ Inserted profile ID: {insertedProfile.Id}");
                    return true;
                }

                Debug.WriteLine("⚠ Insert succeeded but no profile returned.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Insert failed: {ex.Message}");
                return false;
            }
        }




        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
