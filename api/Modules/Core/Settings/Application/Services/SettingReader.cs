using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Core.Settings.Application.Ports;
using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.Modules.Core.Settings.Application.Services;

// Ce qu'un use case injecte quand il a besoin de la valeur d'un paramètre.
//
//     public sealed class SomeUseCase(UseCaseAuthorizer authorizer, SettingReader settings)
//         : SecuredUseCase(authorizer)
//     {
//         public async Task Execute()
//         {
//             var compact = await settings.Get(SettingCatalog.Ui.CompactMode);   // bool
//             var size    = await settings.Get(SettingCatalog.Ui.PageSize);      // long
//         }
//     }
//
// **L'appelant ne passe ni identifiant ni rôle.** `ICurrentUserProvider` les connaît déjà, et
// les faire circuler à la main est le chemin par lequel la règle finit par différer d'un
// appelant à l'autre — l'un oubliant les rôles de module, l'autre comparant le rôle global sur
// un paramètre de module. Le seul argument est *quel paramètre on veut*.
//
// Enregistré en Scoped : le cache ci-dessous vaut pour une requête, pas au-delà.
public sealed class SettingReader(
    ISettingValueRepository repository,
    ICurrentUserProvider currentUserProvider)
{
    // Lignes chargées pour l'audience courante. Un use case qui lit trois paramètres ne fait
    // qu'un aller-retour : le premier appel charge tout le catalogue pour cet appelant, les
    // suivants lisent en mémoire. Le volume est de quelques lignes par personne.
    private readonly Dictionary<string, IReadOnlyList<SettingValue>> loaded = [];

    // Valeur du paramètre pour l'appelant courant.
    public Task<TValue> Get<TValue>(SettingDefinition<TValue> definition) =>
        GetFor(definition, CurrentAudience());

    // Valeur du site, sans appelant. C'est ce qu'utilise une tâche de fond : aucun utilisateur
    // n'y est identifié, donc seules les valeurs globales ont un sens. Le nom le dit, plutôt
    // que de laisser `Get` retomber silencieusement sur le global hors requête HTTP.
    public Task<TValue> GetGlobal<TValue>(SettingDefinition<TValue> definition) =>
        GetFor(definition, SettingAudience.None);

    // Valeur pour quelqu'un d'autre que l'appelant — administration, envoi d'un mail à un
    // destinataire, prévisualisation.
    public async Task<TValue> GetFor<TValue>(SettingDefinition<TValue> definition, SettingAudience audience)
    {
        var resolved = SettingResolution.Resolve(definition, await CandidatesFor(audience), audience);
        return definition.Read(resolved.Value);
    }

    // Tous les paramètres visibles par une audience, avec leur origine et les droits associés.
    // Alimente la page de réglages ; les use cases métier passent par `Get`.
    public async Task<IReadOnlyList<ResolvedSetting>> ResolveVisible(SettingAudience audience)
    {
        var candidates = await CandidatesFor(audience);

        return [.. SettingCatalog.All
            .Where(definition => SettingResolution.CanView(definition, audience))
            .Select(definition => SettingResolution.Resolve(definition, candidates, audience))];
    }

    // À appeler après toute écriture : le cache d'une requête ne doit pas survivre à une
    // modification faite dans cette même requête.
    public void Invalidate() => loaded.Clear();

    private async Task<IReadOnlyList<SettingValue>> CandidatesFor(SettingAudience audience)
    {
        if (loaded.TryGetValue(audience.CacheKey, out var cached))
        {
            return cached;
        }

        var rows = await repository.FindAsync(
            SettingCatalog.AllStoredCodes,
            audience.UserId,
            audience.AllRoleCodes());

        loaded[audience.CacheKey] = rows;
        return rows;
    }

    // Hors requête HTTP, il n'y a aucun appelant : c'est une erreur de programmation, pas un cas
    // à rattraper en silence. Même piège que `SecuredUseCase` construit depuis un scheduler.
    private SettingAudience CurrentAudience()
    {
        var current = currentUserProvider.Current
            ?? throw new InvalidOperationException(
                "Aucun utilisateur identifié : utiliser GetGlobal pour lire un paramètre depuis une tâche de fond.");

        return new SettingAudience(current.UserId, current.Role, current.ModuleRoles);
    }
}
