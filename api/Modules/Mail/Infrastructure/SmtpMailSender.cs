using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Tools.Api.Modules.Mail.Application;
using Tools.Api.Modules.Mail.Application.Ports;
using Tools.Api.Modules.Common.Application.Exceptions;

namespace Tools.Api.Modules.Mail.Infrastructure;

public sealed class SmtpMailSender(
    IOptions<SmtpMailOptions> options,
    IConfiguration environment,
    ILogger<SmtpMailSender> logger) : IMailSender
{
    // Identifiants SMTP partagés avec l'API Java, qui les reçoit sous ces noms : le NAS
    // n'a ainsi qu'une seule convention pour une même valeur. Même approche que JWT_SECRET,
    // DB_HOST ou GOOGLE_CLIENT_ID. Les appsettings restent utilisables en développement.
    private readonly string? username = environment["MAIL_USERNAME"] ?? options.Value.Username;
    private readonly string? password = environment["MAIL_PASSWORD"] ?? options.Value.Password;

    public async Task SendAsync(SendMailCommand command)
    {
        var configuration = options.Value;
        if (string.IsNullOrWhiteSpace(configuration.Host)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(configuration.FromAddress))
        {
            throw AppException.Unavailable("MAIL_NOT_CONFIGURED", "Le service d’envoi d’emails n’est pas configuré.");
        }

        using var email = new System.Net.Mail.MailMessage
        {
            From = new MailAddress(configuration.FromAddress, configuration.FromName),
            Subject = command.Subject,
            Body = command.Html ?? command.Text!,
            IsBodyHtml = command.Html is not null
        };

        foreach (var recipient in command.To)
        {
            email.To.Add(recipient);
        }

        foreach (var attachment in command.Attachments ?? [])
        {
            email.Attachments.Add(new Attachment(
                new MemoryStream(attachment.Content, writable: false),
                attachment.FileName,
                attachment.ContentType));
        }

        using var client = new SmtpClient(configuration.Host, configuration.Port)
        {
            EnableSsl = configuration.EnableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(email);
        logger.LogInformation("Email envoyé recipients={RecipientCount}", command.To.Count);
    }
}

public sealed class SmtpMailOptions
{
    public const string SectionName = "Mail:Smtp";

    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool EnableSsl { get; init; } = true;
    public string? FromAddress { get; init; }
    public string FromName { get; init; } = "Tools - Huiitre";
}
