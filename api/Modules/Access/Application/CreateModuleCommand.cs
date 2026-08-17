namespace Tools.Api.Modules.Access.Application;

// Création d'un module fonctionnel. Il naît toujours inactif : l'activation est un acte
// séparé, pour qu'un module à moitié configuré ne soit jamais visible des utilisateurs.
public sealed record CreateModuleCommand(string Code, string Name, string? Description);
