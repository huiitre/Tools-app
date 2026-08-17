namespace Tools.ApiCore.Modules.Mail.Application;

public sealed record SendMailCommand(
    IReadOnlyCollection<string> To,
    string Subject,
    string? Text = null,
    string? Html = null,
    IReadOnlyCollection<MailAttachment>? Attachments = null);

public sealed record MailAttachment(string FileName, string ContentType, byte[] Content);
