namespace Tools.Api.Modules.Access.Application;

// Mise à jour d'un module fonctionnel, activation comprise.
public sealed record UpdateModuleCommand(
    long ModuleId,
    string Code,
    string Name,
    string? Description,
    bool Active);
