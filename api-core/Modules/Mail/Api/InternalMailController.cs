using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Common.Api.Internal;
using Tools.ApiCore.Modules.Mail.Application;
using Tools.ApiCore.Modules.Mail.Application.Usecases;

namespace Tools.ApiCore.Modules.Mail.Api;

// Envoi de mail par un autre service : l'API Java, le temps de retirer progressivement sa
// propre gestion de l'envoi d'email (voir docs/MAIL.md), puis tout autre appelant du NAS.
//
// AllowAnonymous est indispensable : la FallbackPolicy exige un utilisateur authentifié sur
// toute route non déclarée, et l'appelant ici est une machine qui n'agit au nom de personne.
// C'est InternalApi qui prend le relais du contrôle — même modèle que /internal/notifications.
[ApiController]
[Route("internal/mail")]
[AllowAnonymous]
[InternalApi]
public sealed class InternalMailController(SendInternalMailUseCase sendInternalMailUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send(SendMailRequest request)
    {
        var attachments = request.Attachments?
            .Select(file => new MailAttachment(
                file.FileName,
                file.ContentType,
                MailController.Decode(file.ContentBase64)))
            .ToArray();

        await sendInternalMailUseCase.Execute(
            new SendMailCommand(request.To, request.Subject, request.Text, request.Html, attachments));

        return NoContent();
    }
}
