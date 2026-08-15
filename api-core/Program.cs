using Tools.ApiCore.Composition;
using Tools.ApiCore.Modules.Auth;
using Tools.ApiCore.Modules.Common;
using Tools.ApiCore.Modules.Health;
using Tools.ApiCore.Modules.Mail;
using Tools.ApiCore.Modules.Security;
using Tools.ApiCore.Modules.Users;

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
    .AddUsersModule()
    .AddHealthModule();

var app = builder.Build();

app.UseCorePipeline();

app.MapVersionEndpoint();
app.MapControllers();

// Ces endpoints existent pour les tests d'intégration et ne sont donc mappés dans aucun
// environnement réel — ni Development, ni QA, ni Production.
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapErrorContractTestingEndpoints();
    app.MapUnsecuredTestingEndpoint();
}

app.Run();
