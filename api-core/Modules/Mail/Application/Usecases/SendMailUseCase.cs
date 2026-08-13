// Envoi arbitraire depuis l'adresse de l'application : réservé au niveau technique
// et au-dessus. Les use cases internes qui envoient un mail dans un flux métier
// (réinitialisation de mot de passe, par exemple) n'utilisent pas ce use case mais
// MailService.
public sealed class SendMailUseCase(UseCaseAuthorizer authorizer, MailService mailService)
    : SecuredUseCase<SendMailCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Tech;

    protected override Task Handle(SendMailCommand command, CancellationToken cancellationToken) =>
        mailService.Send(command, cancellationToken);
}
