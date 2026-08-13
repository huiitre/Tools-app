// Identité Google déjà vérifiée cryptographiquement ; elle ne provient jamais directement du navigateur.
public sealed record GoogleIdentity(string ProviderUserId, string Email, string Name, string? PictureUrl);
