using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Settings.Domain;

// La règle : quelle valeur s'applique, à qui, et qui peut la changer.
//
// Fonction pure, sans I/O. Le repository ramène des lignes, ce fichier décide — et se teste
// sans base de données. Mettre cette logique dans une requête SQL l'aurait rendue vérifiable
// seulement avec PostgreSQL, et l'aurait dupliquée au premier autre appelant.
public static class SettingResolution
{
    // Priorité : User > Role > Global > défaut du catalogue.
    //
    // Le verrou inverse l'ordre. Une valeur verrouillée s'impose à tout ce qui est au-dessus
    // d'elle, donc c'est **la plus basse des valeurs verrouillées** qui gagne : un verrou posé
    // en Global interdit aussi bien la valeur de rôle que celle de l'utilisateur.
    public static ResolvedSetting Resolve(
        SettingDefinition definition,
        IEnumerable<SettingValue> candidates,
        SettingAudience audience)
    {
        var applicable = candidates
            .Where(candidate => Targets(definition, candidate, audience))
            .ToList();

        var locked = applicable.Where(candidate => candidate.IsLocked).ToList();

        var winner = locked.Count > 0
            ? locked.MinBy(candidate => candidate.Scope)
            : applicable.MaxBy(candidate => candidate.Scope);

        var role = audience.RoleFor(definition);

        // Poser sa propre valeur suppose trois choses : que le paramètre ait un sens par
        // utilisateur, que le rôle suffise, et qu'aucun verrou ne vienne d'en dessous.
        var canSetOwn =
            definition.AllowedScopes.Contains(SettingScope.User)
            && definition.MinRoleToSetOwn is { } minRole
            && role is { } actual
            && actual.HasAtLeast(minRole)
            && !locked.Any(candidate => candidate.Scope < SettingScope.User);

        return new ResolvedSetting(
            definition,
            winner?.Value ?? definition.DefaultValue,
            winner?.Scope,
            winner?.IsLocked ?? false,
            canSetOwn);
    }

    // L'appelant voit-il ce paramètre ? Seuil et non égalité : un administrateur voit tout ce
    // qu'un modérateur voit. Un rôle absent — typiquement un module auquel l'appelant n'a pas
    // accès — ne satisfait jamais aucun seuil.
    public static bool CanView(SettingDefinition definition, SettingAudience audience) =>
        audience.RoleFor(definition) is { } role && role.HasAtLeast(definition.MinRoleToView);

    // L'appelant peut-il poser une valeur globale ou de rôle ?
    public static bool CanAdminister(SettingDefinition definition, SettingAudience audience) =>
        audience.RoleFor(definition) is { } role && role.HasAtLeast(definition.MinRoleToAdminister);

    // Une ligne stockée s'adresse-t-elle à cet appelant, pour ce paramètre ?
    private static bool Targets(
        SettingDefinition definition,
        SettingValue candidate,
        SettingAudience audience)
    {
        // La ligne concerne-t-elle seulement ce paramètre ? Le contrôle est **ici** et pas chez
        // l'appelant : celui-ci passe volontiers toutes les lignes qu'il a chargées, et un
        // filtrage laissé à sa charge finit par être oublié. Les codes historiques comptent —
        // une valeur écrite sous l'ancien nom reste celle de cette définition.
        if (!definition.AllCodes.Contains(candidate.Code, StringComparer.Ordinal))
        {
            return false;
        }

        // Une portée retirée du catalogue après coup laisse des lignes derrière elle. Les
        // ignorer est le seul comportement sûr : un paramètre d'instance ne doit pas pouvoir
        // être détourné par une ligne User posée du temps où elle était permise.
        if (!definition.AllowedScopes.Contains(candidate.Scope))
        {
            return false;
        }

        // Une contrainte resserrée après coup invalide des valeurs déjà écrites. On retombe
        // sur l'héritage plutôt que de faire échouer la lecture.
        if (!definition.Accepts(candidate.Value))
        {
            return false;
        }

        return candidate.Scope switch
        {
            SettingScope.Global => true,

            // Correspondance **exacte**, à la différence des seuils de permission : une valeur
            // posée sur MODERATOR s'adresse aux modérateurs, pas à tout le monde au-dessus.
            SettingScope.Role => candidate.Role is { } target && target == audience.RoleFor(definition),

            SettingScope.User => candidate.UserId == audience.UserId,

            _ => false
        };
    }
}
