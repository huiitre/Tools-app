namespace Tools.ApiCore.Modules.Auth.Application.Ports.Registration;

// Écritures propres à l'inscription par mot de passe.
public interface IRegistrationRepository
{
    // Compte existant portant cette adresse, ou null.
    Task<RegisteredAccount?> FindAccountByEmailAsync(string email);

    // Crée l'utilisateur, ses credentials, son provider PASSWORD et son rôle USER.
    // Le compte est inactif et son email non vérifié tant que le lien n'a pas été suivi.
    Task<long> CreatePendingUserAsync(string name, string email, string passwordHash);

    // Remplace le mot de passe d'une inscription encore non confirmée.
    Task ReplacePendingPasswordAsync(long userId, string passwordHash);

    // Confirme l'adresse et autorise la connexion.
    Task MarkEmailVerifiedAsync(long userId, DateTime verifiedAt);

    // Adresse du compte, pour les messages destinés aux administrateurs : un identifiant
    // numérique ne dit rien à qui lit la notification.
    Task<string?> FindEmailByIdAsync(long userId);
}

// Projection minimale nécessaire pour décider quoi faire d'une adresse déjà connue.
public sealed record RegisteredAccount(long Id, string Email, bool IsActive, DateTime? EmailVerifiedAt);
