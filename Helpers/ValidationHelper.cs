namespace CATERINGMANAGEMENT.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Strict password validation used by registration and reset flows
        // Rules: minLength, requires upper, lower, digit, special
        public static (bool IsValid, string? Error) ValidatePassword(
            string? password,
            int minLength = 8,
            bool requireUpper = true,
            bool requireLower = true,
            bool requireDigit = true,
            bool requireSpecial = true)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            var p = password;

            if (p.Length < minLength)
                return (false, $"Password must be at least {minLength} characters long.");

            if (requireUpper && !p.Any(char.IsUpper))
                return (false, "Password must include at least one uppercase letter.");

            if (requireLower && !p.Any(char.IsLower))
                return (false, "Password must include at least one lowercase letter.");

            if (requireDigit && !p.Any(char.IsDigit))
                return (false, "Password must include at least one number.");

            if (requireSpecial && !p.Any(c => !char.IsLetterOrDigit(c)))
                return (false, "Password must include at least one special character.");

            return (true, null);
        }
    }
}
