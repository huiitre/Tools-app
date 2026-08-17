namespace Tools.Api.Modules.Mail.Application.Ports;

public interface IMailSender
{
    Task SendAsync(SendMailCommand command);
}
