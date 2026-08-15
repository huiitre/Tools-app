using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Security.Application.Usecases;

// Lecture sécurisée : un use case qui ne reçoit aucune donnée d'entrée et retourne un résultat.
//
// `SecuredUseCase<TCommand, TResult>` ne convient pas ici : il obligerait à déclarer un type
// d'entrée qui n'existe pas. Et le nommer `SecuredUseCase<TResult>` est impossible — en C#,
// l'arité générique fait la signature, et `SecuredUseCase<T>` désigne déjà « une commande sans
// résultat ». D'où une classe au nom distinct plutôt qu'un type vide passé à chaque appel.
//
// La règle pour choisir : ça lit, c'est une SecuredQuery ; ça écrit, c'est un SecuredUseCase.
//
// Le contrôle reste porté par la classe de base : `Execute` n'est pas virtuelle, l'appelant
// validé est passé à `Handle`, et l'oubli de l'autorisation est inexprimable.
public abstract class SecuredQuery<TResult>(UseCaseAuthorizer authorizer)
{
    protected abstract RoleCode RequiredRole { get; }

    public Task<TResult> Execute()
    {
        var currentUser = authorizer.EnsureAtLeast(RequiredRole);
        return Handle(currentUser);
    }

    protected abstract Task<TResult> Handle(CurrentUser currentUser);
}
