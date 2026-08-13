public sealed class SendMailUseCase(MailService mailService)
{
    public Task Execute(SendMailCommand command, CancellationToken cancellationToken) =>
        mailService.Send(command, cancellationToken);
}
