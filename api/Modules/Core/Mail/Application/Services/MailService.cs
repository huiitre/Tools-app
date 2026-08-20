using System.Net.Mail;
using Tools.Api.Modules.Core.Mail.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.Mail.Application.Services;

public sealed class MailService(IMailSender mailSender)
{
    public async Task Send(
        SendMailCommand command)
    {
        Validate(command);
        await mailSender.SendAsync(command);
    }

    private static void Validate(SendMailCommand command)
    {
        if (command.To.Count == 0 || command.To.Any(address => !IsEmail(address)))
        {
            throw AppException.Validation("INVALID_MAIL_RECIPIENT", "Au moins une adresse email est invalide.");
        }

        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            throw AppException.Validation("INVALID_MAIL_SUBJECT", "Le sujet de l’email est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(command.Text) && string.IsNullOrWhiteSpace(command.Html))
        {
            throw AppException.Validation("MISSING_MAIL_BODY", "Un contenu texte ou HTML est obligatoire.");
        }
    }

    private static bool IsEmail(string value)
    {
        try
        {
            return new MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
