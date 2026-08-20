namespace Tools.Api.Modules.Core.Mail.Application.Ports;

public interface IMailSender
{
    Task SendAsync(SendMailCommand command);
}
