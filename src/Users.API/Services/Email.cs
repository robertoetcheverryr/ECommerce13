namespace Users.API.Services;

public static class Email
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        try
        {
            var parsed = new System.Net.Mail.MailAddress(trimmed);
            return parsed.Address.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string Normalize(string value) => value.Trim();
}
