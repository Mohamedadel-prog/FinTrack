
namespace FinTrack.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP host. When empty, emails are written to the log instead of sent.</summary>
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "no-reply@fintrack.local";

    public string FromName { get; set; } = "FinTrack";
}
