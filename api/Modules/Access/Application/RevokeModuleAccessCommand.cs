namespace Tools.ApiCore.Modules.Access.Application;

// Retirer l'accès d'un utilisateur à un module.
public sealed record RevokeModuleAccessCommand(long ModuleId, long UserId);
