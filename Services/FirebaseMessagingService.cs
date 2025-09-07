using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Threading.Tasks;

public class FirebaseMessagingService
{
    private static FirebaseMessagingService? _instance;
    private readonly FirebaseMessaging _messaging;

    private FirebaseMessagingService()
    {
        var app = FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile("Credentials/service-account-file.json")
        });

        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public static FirebaseMessagingService Instance
    {
        get
        {
            if (_instance == null)
                _instance = new FirebaseMessagingService();
            return _instance;
        }
    }

    public async Task<string> SendNotificationAsync(string deviceToken, string title, string body)
    {
        var message = new Message()
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            }
        };

        return await _messaging.SendAsync(message);
    }
}
