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

        // Optional: store user only (note: this does NOT include access/refresh tokens)
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
