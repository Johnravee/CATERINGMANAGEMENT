using CATERINGMANAGEMENT.Services;
using CATERINGMANAGEMENT.View;
using CATERINGMANAGEMENT.View.Windows;
using System.Windows;
using System.Windows.Controls;

public static class AuthGuard
{
    public static bool RequireAuthentication(Window currentWindow)
    {
        if (SessionService.CurrentSession == null)
        {
            MessageBox.Show("Access denied. Please log in first.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);

            var login = new LoginView();
            login.Show();

            currentWindow.Close();
            return false;
        }

        return true;
    }

    public static bool RequireAuthentication(Page currentPage)
    {
        if (SessionService.CurrentSession == null)
        {
            MessageBox.Show("Access denied. Please log in first.", "Unauthorized", MessageBoxButton.OK, MessageBoxImage.Warning);

            var login = new LoginView();
            login.Show();


            Window parentWindow = Window.GetWindow(currentPage);
            parentWindow?.Close();

            return false;
        }

        return true;
    }

    public static void PreventAccessIfAuthenticated(Window currentWindow)
    {
        if (SessionService.CurrentSession != null)
        {
          
            var dashboard = new Dashboard();
            dashboard.Show();
            currentWindow.Close();
        }
    }
}
