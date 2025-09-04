using Supabase.Gotrue;

namespace CATERINGMANAGEMENT.Services
{
    public static class SessionService
    {
        private static Session? _currentSession;

        public static Session? CurrentSession => _currentSession;
        public static User? CurrentUser => _currentSession?.User;

        public static void SetSession(Session session)
        {
            _currentSession = session;
        }

        // Optional: if you just want to store the user separately
        public static void SetCurrentUser(User user)
        {
            _currentSession = new Session { User = user };
        }

        public static void ClearSession()
        {
            _currentSession = null;
        }

        public static bool IsLoggedIn => _currentSession?.User != null && !_currentSession.Expired();
    }
}
