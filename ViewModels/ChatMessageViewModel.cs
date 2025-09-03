using CATERINGMANAGEMENT.Models;
using CATERINGMANAGEMENT.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CATERINGMANAGEMENT.ViewModels
{
    public class ChatMessageViewModel : INotifyPropertyChanged
    {
        private readonly Profile _profile;

        public ChatMessageViewModel(Profile profile)
        {
            _profile = profile;
            Debug.WriteLine($"👤 ChatMessageViewModel created for Profile ID={_profile.Id}, Name={_profile.FullName}");
        }

        private ObservableCollection<Message> _messages = new();
        public ObservableCollection<Message> Messages
        {
            get => _messages;
            set
            {
                _messages = value;
                OnPropertyChanged();
            }
        }

        private string _newMessage;
        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadMessages()
        {
            Debug.WriteLine($"📥 Loading messages for Profile ID={_profile.Id}");

            var client = await SupabaseService.GetClientAsync();

            var response = await client
                .From<Message>()
                .Select("*, Sender:sender_id(*), Receiver:receiver_id(*)")
                .Where(m => m.ReceiverId == _profile.Id || m.SenderId == _profile.Id)
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            Messages.Clear();

            if (response.Models != null)
            {
                Profile? previousSender = null;

                foreach (var msg in response.Models)
                {
                    // ✅ Determine if we need to show the sender header
                    msg.ShowSenderHeader = previousSender == null || msg.Sender?.Id != previousSender.Id;
                    previousSender = msg.Sender;

                    Messages.Add(msg);

                    Debug.WriteLine($"📩 Message: Id={msg.Id}, From={msg.Sender?.FullName}, To={msg.Receiver?.FullName}, Content='{msg.Content}', CreatedAt={msg.CreatedAt}, ShowHeader={msg.ShowSenderHeader}");
                }
            }
            else
            {
                Debug.WriteLine($"⚠ No messages found for Profile ID {_profile.Id}");
            }
        }



        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
