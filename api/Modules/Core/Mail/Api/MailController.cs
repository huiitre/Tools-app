using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Tools.Api.Modules.Core.Mail.Application.Usecases;
using Tools.Api.Modules.Core.Mail.Application;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.Mail.Api;

[ApiController]
[Route("mail")]
public sealed class MailController : ControllerBase
{
    // Résolu par action ([FromServices]) : le use case applique son contrôle — TECH — dès sa
    // construction, il ne doit donc être construit que par la route qui s'en sert.
    [HttpPost]
    public async Task<IActionResult> Send(
        SendMailRequest request,
        [FromServices] SendMailUseCase sendMailUseCase)
    {
        var attachments = request.Attachments?
            .Select(file => new MailAttachment(file.FileName, file.ContentType, Decode(file.ContentBase64)))
            .ToArray();

        await sendMailUseCase.Execute(
            new SendMailCommand(request.To, request.Subject, request.Text, request.Html, attachments));

        return NoContent();
    }

    internal static byte[] Decode(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw AppException.Validation(
                "INVALID_MAIL_ATTACHMENT_CONTENT",
                "Le contenu d’une pièce jointe doit être encodé en Base64 valide.");
        }
    }
}

public sealed record SendMailRequest(
    [param: Required, MinLength(1)] IReadOnlyCollection<string> To,
    [param: Required] string Subject,
    string? Text = null,
    string? Html = null,
    IReadOnlyCollection<SendMailAttachmentRequest>? Attachments = null);

public sealed record SendMailAttachmentRequest(
    [param: Required] string FileName,
    [param: Required] string ContentType,
    [param: Required] string ContentBase64);
