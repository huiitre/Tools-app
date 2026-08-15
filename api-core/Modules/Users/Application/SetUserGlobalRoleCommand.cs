namespace Tools.ApiCore.Modules.Users.Application;

// Attribution du rôle global d'un utilisateur. Le rôle est désigné par son identifiant, comme
// dans l'API Java : le frontend l'a lu dans le catalogue GET /roles.
public sealed record SetUserGlobalRoleCommand(long UserId, long RoleId);
