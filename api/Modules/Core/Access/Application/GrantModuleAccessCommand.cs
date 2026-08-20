namespace Tools.Api.Modules.Core.Access.Application;

// Ouvrir l'accès d'un utilisateur à un module. Le rôle initial est READ_ONLY, comme dans
// l'API Java : donner un accès ne donne jamais un pouvoir.
public sealed record GrantModuleAccessCommand(long ModuleId, long UserId);
