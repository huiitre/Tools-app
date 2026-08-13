// Use case dont l'exécution exige un rôle minimum.
//
// Le contrôle est porté par la classe de base plutôt que par un marquage : `Execute`
// n'est pas virtuelle, une classe dérivée ne peut donc ni l'outrepasser ni oublier
// d'appeler l'autorisation. Un use case sécurisé ne peut pas exister sans son contrôle.
//
// Ces use cases ne doivent pas être appelés hors d'une requête authentifiée : depuis
// une tâche de fond, aucun utilisateur n'est identifié et l'exécution est refusée.
public abstract class SecuredUseCase<TCommand>(UseCaseAuthorizer authorizer)
{
    protected abstract RoleCode RequiredRole { get; }

    public Task Execute(TCommand command, CancellationToken cancellationToken)
    {
        authorizer.EnsureAtLeast(RequiredRole);
        return Handle(command, cancellationToken);
    }

    protected abstract Task Handle(TCommand command, CancellationToken cancellationToken);
}

// Variante pour les use cases qui retournent un résultat.
public abstract class SecuredUseCase<TCommand, TResult>(UseCaseAuthorizer authorizer)
{
    protected abstract RoleCode RequiredRole { get; }

    public Task<TResult> Execute(TCommand command, CancellationToken cancellationToken)
    {
        authorizer.EnsureAtLeast(RequiredRole);
        return Handle(command, cancellationToken);
    }

    protected abstract Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}
