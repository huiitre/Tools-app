using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Common.Api.Internal;
using Tools.ApiCore.Modules.Mail.Application;
using Tools.ApiCore.Modules.Mail.Application.Usecases;

namespace Tools.ApiCore.Modules.Mail.Api;

// Envoi de mail pour un appelant de service à service, authentifié par secret partagé et non par jeton utilisateur.
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
