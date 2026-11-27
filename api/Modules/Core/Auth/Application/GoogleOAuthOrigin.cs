namespace Tools.Api.Modules.Core.Auth.Application;

// Client qui a démarré la connexion Google, et le port local où Electron attend le token.
public sealed record GoogleOAuthOrigin(string Source, int? LoopbackPort);
