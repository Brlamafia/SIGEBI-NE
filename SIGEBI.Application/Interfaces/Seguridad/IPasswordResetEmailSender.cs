namespace SIGEBI.Application.Interfaces.Seguridad;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(
        string recipientEmail,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
