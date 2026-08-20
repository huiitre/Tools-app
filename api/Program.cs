using Tools.Api.Composition;
using Tools.Api.Modules.Access;
using Tools.Api.Modules.Admin;
using Tools.Api.Modules.Auth;
using Tools.Api.Modules.Common;
using Tools.Api.Modules.Health;
using Tools.Api.Modules.GameServers;
using Tools.Api.Modules.Mail;
using Tools.Api.Modules.Notifications;
using Tools.Api.Modules.Realtime;
using Tools.Api.Modules.Security;
using Tools.Api.Modules.Users;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreHost();

// Racine de composition : elle voit tous les modules, et rien d'autre. Chaque module déclare
// lui-même ses ports, ses use cases et ses options — voir Modules/<Module>/<Module>Module.cs.
// Common vient en premier : les autres dépendent de son contrat d'erreur et de son accès
// PostgreSQL, jamais l'inverse.
builder.AddCommonModule()
    .AddSecurityModule()
    .AddAuthModule()
    .AddMailModule()
    .AddRealtimeModule()
    .AddNotificationsModule()
    .AddUsersModule()
    .AddAccessModule()
    .AddAdminModule()
    .AddHealthModule()
    .AddGameServersModule();

var app = builder.Build();

app.UseCorePipeline();

app.MapVersionEndpoint();
app.MapControllers();
app.MapRealtimeModule();

// Ces endpoints existent pour les tests d'intégration et ne sont donc mappés dans aucun
// environnement réel — ni Development, ni QA, ni Production.
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapErrorContractTestingEndpoints();
    app.MapUnsecuredTestingEndpoint();
    app.MapModuleAuthorizationTestingEndpoint();
}

app.Run();
