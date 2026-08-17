namespace Tools.Api.Modules.Auth.Infrastructure.Password;

// Paramètres du flux de réinitialisation de mot de passe.
// Valeurs alignées sur l'API Java : jeton de 32 octets, valable 30 minutes.
public sealed class PasswordResetOptions
{
    public const string SectionName = "Auth:PasswordReset";

    public int TokenBytes { get; init; } = 32;

    public int TokenTtlMinutes { get; init; } = 30;

    // Chemin de la page front qui reçoit le jeton.
    public string ResetPath { get; init; } = "/auth/reset-password";
}
