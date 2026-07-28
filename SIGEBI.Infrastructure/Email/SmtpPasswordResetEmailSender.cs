using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SIGEBI.Application.Interfaces.Seguridad;
using SIGEBI.Application.Options;

namespace SIGEBI.Infrastructure.Email;

public sealed class SmtpPasswordResetEmailSender(
    IOptions<SmtpOptions> options) : IPasswordResetEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public bool IsConfigured => _options.IsConfigured;

    public async Task SendAsync(
        string recipientEmail,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "El servidor SMTP no está configurado.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _options.FromName,
            _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "Restablece tu contraseña de SIGEBI";
        message.Body = new BodyBuilder
        {
            TextBody =
                $"""
                Recibimos una solicitud para restablecer tu contraseña de SIGEBI.

                Abre el siguiente enlace:
                {resetUrl}

                El enlace expirará en 30 minutos y solo puede utilizarse una vez.
                Si no realizaste esta solicitud, puedes ignorar este correo.
                """,
            HtmlBody =
                $"""
                <!doctype html>
                <html lang="es">
                <body style="margin:0;background:#f5f8ff;font-family:Arial,sans-serif;color:#101936">
                  <div style="max-width:560px;margin:32px auto;background:#fff;border:1px solid #dbe4f5;border-radius:16px;overflow:hidden">
                    <div style="padding:24px 32px;background:#023877;color:#fff">
                      <strong style="font-size:20px">SIGEBI Nueva Era</strong>
                    </div>
                    <div style="padding:32px">
                      <h1 style="margin:0 0 16px;font-size:26px">Restablece tu contraseña</h1>
                      <p style="line-height:1.6">Recibimos una solicitud para cambiar la contraseña de tu cuenta.</p>
                      <p style="margin:28px 0">
                        <a href="{resetUrl}" style="display:inline-block;padding:13px 20px;border-radius:9px;background:#286CF7;color:#fff;text-decoration:none;font-weight:bold">
                          Crear nueva contraseña
                        </a>
                      </p>
                      <p style="line-height:1.6;color:#69738d">El enlace expirará en 30 minutos y solo puede utilizarse una vez.</p>
                      <p style="line-height:1.6;color:#69738d">Si no realizaste esta solicitud, ignora este mensaje.</p>
                    </div>
                  </div>
                </body>
                </html>
                """
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            ResolveSecurity(_options.Security),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions ResolveSecurity(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "none" => SecureSocketOptions.None,
            "ssl" or "ssl-on-connect" =>
                SecureSocketOptions.SslOnConnect,
            "starttls-when-available" =>
                SecureSocketOptions.StartTlsWhenAvailable,
            _ => SecureSocketOptions.StartTls
        };
}
