namespace Tools.Api.Modules.Core.Access.Application;

// Mise à jour d'un module fonctionnel, activation comprise.
public sealed record UpdateModuleCommand(
    long ModuleId,
    string Code,
    string Name,
    string? Description,
    bool Active);
