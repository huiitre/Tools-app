namespace Tools.ApiCore.Modules.Mail.Application.Ports;

public interface IMailSender
{
    Task SendAsync(SendMailCommand command, CancellationToken cancellationToken);
}
