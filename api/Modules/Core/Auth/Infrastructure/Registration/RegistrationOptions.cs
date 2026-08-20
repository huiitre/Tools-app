namespace Tools.Api.Modules.Core.Auth.Infrastructure.Registration;

// Paramètres du flux d'inscription. Valeurs alignées sur l'API Java.
public sealed class RegistrationOptions
{
    public const string SectionName = "Auth:Registration";

    public int TokenBytes { get; init; } = 32;

    public int TokenTtlMinutes { get; init; } = 30;

    // Chemin de la page front qui reçoit le jeton de confirmation.
    public string VerifyPath { get; init; } = "/auth/verify-email";
}
