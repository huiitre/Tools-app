namespace Tools.ApiCore.Modules.Access.Application;

// Changer le rôle d'un membre à l'intérieur d'un module.
public sealed record ChangeModuleRoleCommand(long ModuleId, long UserId, long RoleId);
