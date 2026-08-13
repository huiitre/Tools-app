public interface IMailSender
{
    Task SendAsync(SendMailCommand command, CancellationToken cancellationToken);
}
