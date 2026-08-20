using Tools.Api.Modules.Core.Auth.Domain;

namespace Tools.Api.IntegrationTests.Fakes;

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

    // Clé : jeton de confirmation d'adresse
    public Dictionary<string, (long UserId, DateTime ExpiresAt)> VerificationTokens { get; } = [];

    // Inscriptions : identité minimale et état de confirmation.
    public Dictionary<long, (string Name, string Email, bool IsActive, DateTime? EmailVerifiedAt)> Accounts { get; } = [];

    public void Reset()
    {
        Users.Clear();
        Providers.Clear();
        PasswordHashes.Clear();
        ResetTokens.Clear();
        VerificationTokens.Clear();
        Accounts.Clear();
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

        Accounts[id] = ("Utilisateur", email, true, DateTime.UtcNow);

        return user;
    }
}
