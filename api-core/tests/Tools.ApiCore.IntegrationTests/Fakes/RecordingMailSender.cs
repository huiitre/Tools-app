using Tools.ApiCore.Modules.Mail.Application;
using Tools.ApiCore.Modules.Mail.Application.Ports;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// Remplace l'envoi SMTP : la commande reçue est conservée pour être inspectée par le test.
public sealed class RecordingMailSender : IMailSender
{
    public SendMailCommand? LastCommand { get; private set; }

    public Task SendAsync(SendMailCommand command, CancellationToken cancellationToken)
    {
        LastCommand = command;
        return Task.CompletedTask;
    }

    public void Clear() => LastCommand = null;
}
