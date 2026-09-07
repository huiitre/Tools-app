namespace Tools.Api.Modules.Core.Users.Application;

// Autorisation de connexion d'un compte. L'état voulu est transmis explicitement plutôt que
// laissé à un basculement calculé par le serveur : deux clics rapides sur le même compte
// aboutiraient sinon à l'état inverse de celui affiché à l'écran.
public sealed record SetUserActiveCommand(long UserId, bool Active);
