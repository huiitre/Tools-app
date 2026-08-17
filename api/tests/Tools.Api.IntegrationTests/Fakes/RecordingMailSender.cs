using Tools.Api.Modules.Mail.Application;
using Tools.Api.Modules.Mail.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Remplace l'envoi SMTP : la commande reçue est conservée pour être inspectée par le test.
public sealed class RecordingMailSender : IMailSender
{
    public SendMailCommand? LastCommand { get; private set; }

    public Task SendAsync(SendMailCommand command)
    {
        LastCommand = command;
        return Task.CompletedTask;
    }

    public void Clear() => LastCommand = null;
}
