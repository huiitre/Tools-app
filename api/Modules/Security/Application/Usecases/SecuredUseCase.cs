using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Domain;

namespace Tools.Api.Modules.Security.Application.Usecases;

// Use case dont l'exécution exige un appelant identifié et un rôle minimum.
//
// **Hériter suffit.** Le contrôle est appliqué par le constructeur de cette classe, et le
// constructeur d'une classe de base s'exécute toujours, avant que l'objet dérivé existe : il n'y
// a aucun moyen de construire un use case sécurisé sans que son droit d'accès ait été vérifié.
// Ce n'est pas une convention à respecter, c'est le langage.
//
// En contrepartie, cette classe n'impose **aucune méthode** à ses héritiers : la méthode métier
// porte le nom, les arguments et le type de retour que le use case veut. C'est ce qui remplace
// les anciennes `SecuredQuery<TResult>` / `SecuredUseCase<TCommand>` / `SecuredUseCase<TCommand,
// TResult>`, où le choix de la classe de base dépendait de la forme de la signature.
//
// Sans rien déclarer, le use case exige un compte portant au moins READ_ONLY : un appel anonyme
// et un compte sans rôle sont refusés par défaut. Un use case plus exigeant surcharge
// `RequiredRole`, un use case qui appartient à un module surcharge `RequiredModule`.
//
// Le contrôle ne dépend d'aucun middleware HTTP : il vaut aussi pour un appel venu d'un hub
// SignalR. Ces use cases ne doivent en revanche pas être construits depuis une tâche de fond —
// aucun utilisateur n'y est identifié, l'autorisation échoue.
public abstract class SecuredUseCase
{
    protected SecuredUseCase(UseCaseAuthorizer authorizer)
    {
        CurrentUser = authorizer.EnsureAtLeast(RequiredRole, RequiredModule);
    }

    // L'appelant validé : le use case dispose de son identité sans avoir à la résoudre lui-même,
    // et sans jamais manipuler une valeur nulle.
    protected CurrentUser CurrentUser { get; }

    // Rôle minimum exigé. READ_ONLY par défaut : le plus bas de la hiérarchie, donc « un compte,
    // avec un rôle, quel qu'il soit ».
    //
    // À déclarer en valeur littérale (`=> RoleCode.Admin`) : la propriété est lue pendant la
    // construction de cette classe de base, avant le constructeur de la classe dérivée. Une
    // valeur calculée à partir d'un champ de la classe dérivée serait lue avant que ce champ soit
    // renseigné. Le rôle exigé caractérise le use case, jamais son état — la règle est donc
    // sans effet pratique, mais elle explique pourquoi on ne la contourne pas.
    protected virtual RoleCode RequiredRole => RoleCode.ReadOnly;

    // Module auquel appartient le use case. Déclaré, le rôle exigé se lit dans ce module et non
    // parmi les rôles globaux. Absent par défaut : les use cases transverses du Core
    // (administration, compte, mail) ne relèvent d'aucun module. Même règle de valeur littérale
    // que `RequiredRole`.
    protected virtual ModuleCode? RequiredModule => null;
}
