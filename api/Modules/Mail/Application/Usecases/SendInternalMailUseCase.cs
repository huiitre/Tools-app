using Tools.ApiCore.Modules.Mail.Application.Services;

namespace Tools.ApiCore.Modules.Mail.Application.Usecases;

// Envoi de mail déclenché par un appel de service à service, sans utilisateur à autoriser.
public sealed class SendInternalMailUseCase(MailService mailService)
{
    public Task Execute(SendMailCommand command) => mailService.Send(command);
}
