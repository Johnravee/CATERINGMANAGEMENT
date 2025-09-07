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

            if(profile == null)
                throw new ArgumentNullException(nameof(profile), "Profile cannot be null when initializing ChatMessageViewModel.");

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

        //public async Task SendMessageAsync()
        //{
        //    if (string.IsNullOrWhiteSpace(NewMessage))
        //        return;

        //    if (_profile == null)
        //    {
        //        Debug.WriteLine("⚠ Profile is null. Cannot send message.");
        //        return;
        //    }

        //    var client = await SupabaseService.GetClientAsync();

        //    // You need to know who the current sender is (maybe from auth context)
        //    var currentUser = await SessionService.CurrentUser.Aud ; // ← implement this if needed

        //    if (currentUser == null)
        //    {
        //        Debug.WriteLine("⚠ Current user not authenticated.");
        //        return;
        //    }

        //    var message = new Message
        //    {
        //        SenderId = currentUser.Id,
        //        ReceiverId = _profile.Id,
        //        Content = NewMessage,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    var response = await client
        //        .From<Message>()
        //        .Insert(message);

        //    if (response.Models == null || response.Models.Count == 0)
        //    {
        //        Debug.WriteLine("❌ Failed to insert message.");
        //        return;
        //    }

        //    var savedMessage = response.Models.First();
        //    savedMessage.Sender = currentUser;
        //    savedMessage.Receiver = _profile;
        //    savedMessage.ShowSenderHeader = Messages.LastOrDefault()?.SenderId != currentUser.Id;

        //    Messages.Add(savedMessage);

        //    // Clear the message box
        //    NewMessage = string.Empty;

        //    Debug.WriteLine($"✅ Message sent: {savedMessage.Content}");

        //    // 🔔 Send Firebase push notification
        //    try
        //    {
        //        var token = _profile.FcmToken; 
        //        if (!string.IsNullOrEmpty(token))
        //        {
        //            await FirebaseMessagingService.Instance.SendNotificationAsync(
        //                token,
        //                $"New message from {currentUser.FullName}",
        //                message.Content
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"❌ Firebase notification failed: {ex.Message}");
        //    }
        //}


        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
