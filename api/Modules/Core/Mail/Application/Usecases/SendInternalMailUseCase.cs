using Tools.Api.Modules.Core.Mail.Application.Services;

namespace Tools.Api.Modules.Core.Mail.Application.Usecases;

// Envoi de mail déclenché par un appel de service à service, sans utilisateur à autoriser.
public sealed class SendInternalMailUseCase(MailService mailService)
{
    public Task Execute(SendMailCommand command) => mailService.Send(command);
}
