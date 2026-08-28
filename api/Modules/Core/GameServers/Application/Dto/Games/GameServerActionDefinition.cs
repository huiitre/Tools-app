using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Action d'administration déclarée par un jeu. Le front n'en connaît aucune : il rend un
// formulaire à partir de cette description, et n'affiche que celles que l'utilisateur peut
// déclencher. Un jeu qui n'expose que « save » n'en déclare qu'une.
public sealed record GameServerActionDefinition(
    string Code,
    string Label,
    string Icon,
    // Rôle minimum exigé, vérifié côté serveur au moment de l'exécution : ce que le front affiche
    // n'autorise rien.
    RoleCode Role,
    // Marque les actions qui coupent le serveur ou bannissent : le front les signale en rouge.
    bool Dangerous,
    IReadOnlyList<GameServerActionParameter> Parameters);
