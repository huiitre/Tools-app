using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("mail")]
public sealed class MailController(SendMailUseCase sendMailUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send(SendMailRequest request, CancellationToken cancellationToken)
    {
        var attachments = request.Attachments?
            .Select(file => new MailAttachment(file.FileName, file.ContentType, Decode(file.ContentBase64)))
            .ToArray();

        await sendMailUseCase.Execute(
            new SendMailCommand(request.To, request.Subject, request.Text, request.Html, attachments),
            cancellationToken);

        return NoContent();
    }

    private static byte[] Decode(string value)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw ApplicationException.Validation(
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
