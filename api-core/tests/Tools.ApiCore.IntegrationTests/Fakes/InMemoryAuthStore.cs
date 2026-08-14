using Tools.ApiCore.Modules.Auth.Domain;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// État partagé par tous les doubles mémoire du module Auth : il tient lieu de base de
// données pour les tests, qui n'ouvrent aucune connexion PostgreSQL.
public sealed class InMemoryAuthStore
{
    public Dictionary<long, AuthUser> Users { get; } = [];

    // Clé : (userId, provider)
    public HashSet<(long UserId, string Provider)> Providers { get; } = [];

    public Dictionary<long, string> PasswordHashes { get; } = [];

    // Clé : jeton
    public Dictionary<string, (long UserId, DateTime ExpiresAt)> ResetTokens { get; } = [];

    public void Reset()
    {
        Users.Clear();
        Providers.Clear();
        PasswordHashes.Clear();
        ResetTokens.Clear();
    }

    public AuthUser AddUser(long id, string email, bool withPasswordProvider)
    {
        var user = new AuthUser(id, email, true, "HUMAN");
        Users[id] = user;
        if (withPasswordProvider)
        {
            Providers.Add((id, "PASSWORD"));
            PasswordHashes[id] = "hash-existant";
        }

        return user;
    }
}
