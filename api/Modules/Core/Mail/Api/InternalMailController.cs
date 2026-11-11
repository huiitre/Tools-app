using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.Mail.Application;
using Tools.Api.Modules.Core.Mail.Application.Usecases;

namespace Tools.Api.Modules.Core.Mail.Api;

// Ancienne URL des scripts du NAS, gardée le temps de leur migration : même use case que
// POST /mail. À supprimer quand plus aucun script ne l'appelle.
[ApiController]
[Route("internal/mail")]
public sealed class InternalMailController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send(
        SendMailRequest request,
        [FromServices] SendMailUseCase sendMailUseCase)
    {
        var attachments = request.Attachments?
            .Select(file => new MailAttachment(
                file.FileName,
                file.ContentType,
                MailController.Decode(file.ContentBase64)))
            .ToArray();

        await sendMailUseCase.Execute(
            new SendMailCommand(request.To, request.Subject, request.Text, request.Html, attachments));

        return NoContent();
    }
}
