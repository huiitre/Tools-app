// Port d'identification de l'appelant : l'Application ignore d'où vient l'identité
// (en-tête HTTP, token, autre) et ne connaît que son identifiant et ses rôles.
public interface ICurrentUserProvider
{
    // Appelant authentifié, ou null si l'appel est anonyme ou effectué hors d'une
    // requête HTTP (tâche de fond, par exemple).
    CurrentUser? Current { get; }
}

// Les rôles proviennent de l'access token et font autorité pendant sa durée de vie :
// un droit retiré s'applique au renouvellement du token, pas immédiatement.
public sealed record CurrentUser(long UserId, IReadOnlyCollection<RoleCode> Roles)
{
    // Rôle effectif : le plus permissif de ceux portés par le token.
    public RoleCode? HighestRole => Roles.Count == 0 ? null : Roles.Max();
}
