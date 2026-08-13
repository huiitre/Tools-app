using System.Net.Mail;

public sealed class MailService(IMailSender mailSender)
{
    public async Task Send(
        SendMailCommand command,
        CancellationToken cancellationToken)
    {
        Validate(command);
        await mailSender.SendAsync(command, cancellationToken);
    }

    private static void Validate(SendMailCommand command)
    {
        if (command.To.Count == 0 || command.To.Any(address => !IsEmail(address)))
        {
            throw ApplicationException.Validation("INVALID_MAIL_RECIPIENT", "Au moins une adresse email est invalide.");
        }

        if (string.IsNullOrWhiteSpace(command.Subject))
        {
            throw ApplicationException.Validation("INVALID_MAIL_SUBJECT", "Le sujet de l’email est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(command.Text) && string.IsNullOrWhiteSpace(command.Html))
        {
            throw ApplicationException.Validation("MISSING_MAIL_BODY", "Un contenu texte ou HTML est obligatoire.");
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
