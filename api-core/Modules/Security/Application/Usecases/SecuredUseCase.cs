using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Security.Application.Usecases;

// Use case dont l'exécution exige un rôle minimum.
//
// Le contrôle est porté par la classe de base plutôt que par un marquage : `Execute`
// n'est pas virtuelle, une classe dérivée ne peut donc ni l'outrepasser ni oublier
// d'appeler l'autorisation. Un use case sécurisé ne peut pas exister sans son contrôle.
//
// L'appelant validé est passé à `Handle` : le use case dispose de son identité sans
// avoir à la résoudre lui-même, et sans jamais manipuler une valeur nulle.
//
// Ces use cases ne doivent pas être appelés hors d'une requête authentifiée : depuis
// une tâche de fond, aucun utilisateur n'est identifié et l'exécution est refusée.
public abstract class SecuredUseCase<TCommand>(UseCaseAuthorizer authorizer)
{
    protected abstract RoleCode RequiredRole { get; }

    public Task Execute(TCommand command)
    {
        var currentUser = authorizer.EnsureAtLeast(RequiredRole);
        return Handle(command, currentUser);
    }

    protected abstract Task Handle(TCommand command, CurrentUser currentUser);
}

// Variante pour les use cases qui retournent un résultat.
public abstract class SecuredUseCase<TCommand, TResult>(UseCaseAuthorizer authorizer)
{
    protected abstract RoleCode RequiredRole { get; }

    public Task<TResult> Execute(TCommand command)
    {
        var currentUser = authorizer.EnsureAtLeast(RequiredRole);
        return Handle(command, currentUser);
    }

    protected abstract Task<TResult> Handle(TCommand command, CurrentUser currentUser);
}
