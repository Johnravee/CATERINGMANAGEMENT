using System.Windows;
using System.Windows.Controls;

namespace CATERINGMANAGEMENT.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d)
            => (string)d.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject d, string value)
            => d.SetValue(BoundPasswordProperty, value);

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        public static bool GetBindPassword(DependencyObject dp)
            => (bool)dp.GetValue(BindPasswordProperty);

        public static void SetBindPassword(DependencyObject dp, bool value)
            => dp.SetValue(BindPasswordProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox box && !GetUpdatingPassword(box))
            {
                box.PasswordChanged -= HandlePasswordChanged;
                var newPassword = (string)e.NewValue ?? string.Empty;
                if (box.Password != newPassword)
                {
                    box.Password = newPassword;
                }
                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox box)
            {
                bool wasBound = (bool)e.OldValue;
                bool needToBind = (bool)e.NewValue;

                if (wasBound)
                {
                    box.PasswordChanged -= HandlePasswordChanged;
                }

                if (needToBind)
                {
                    box.PasswordChanged += HandlePasswordChanged;
                }
            }
        }

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxHelper));

        private static bool GetUpdatingPassword(DependencyObject dp)
            => (bool)dp.GetValue(UpdatingPasswordProperty);

        private static void SetUpdatingPassword(DependencyObject dp, bool value)
            => dp.SetValue(UpdatingPasswordProperty, value);

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox box)
            {
                SetUpdatingPassword(box, true);
                SetBoundPassword(box, box.Password);
                SetUpdatingPassword(box, false);
            }
        }
    }
}
