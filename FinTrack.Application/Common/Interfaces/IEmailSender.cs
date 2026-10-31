
namespace FinTrack.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
