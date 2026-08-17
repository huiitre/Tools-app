using Tools.Api.Modules.Mail.Application.Services;
using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Mail.Application.Usecases;

// Envoi arbitraire depuis l'adresse de l'application : réservé au niveau technique
// et au-dessus. Les use cases internes qui envoient un mail dans un flux métier
// (réinitialisation de mot de passe, par exemple) n'utilisent pas ce use case mais
// MailService.
public sealed class SendMailUseCase(UseCaseAuthorizer authorizer, MailService mailService)
    : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Tech;

    public Task Execute(SendMailCommand command)
    {
        return mailService.Send(command);
    }
}
