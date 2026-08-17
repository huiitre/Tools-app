using Tools.ApiCore.Modules.Mail.Application.Services;

namespace Tools.ApiCore.Modules.Mail.Application.Usecases;

// Action déclenchée par un autre service (l'API Java, un extracteur du NAS...), sans
// utilisateur courant à autoriser : le secret partagé de /internal/mail a déjà tranché
// l'accès avant que ce use case ne s'exécute — voir InternalApiAttribute.
//
// Ce n'est volontairement pas un SecuredUseCase : cette famille exige un CurrentUser résolu
// depuis une requête authentifiée (voir SecuredUseCase.cs), qui n'existe pas pour un appelant
// machine. L'absence de rôle ou de module requis ne dispense pas pour autant l'endpoint
// d'appeler un use case : c'est lui qui porte l'action, jamais le contrôleur.
public sealed class SendInternalMailUseCase(MailService mailService)
{
    public Task Execute(SendMailCommand command) => mailService.Send(command);
}
