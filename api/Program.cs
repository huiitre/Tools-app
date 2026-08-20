using Tools.Api.Composition;
using Tools.Api.Modules.Core;
using Tools.Api.Modules.Core.Auth;
using Tools.Api.Modules.Core.Common;
using Tools.Api.Modules.Core.Health;
using Tools.Api.Modules.Core.Realtime;
using Tools.Api.Modules.Core.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddCoreHost();

// Racine de composition : elle voit tous les modules, et rien d'autre. Chaque module déclare
// lui-même ses ports, ses use cases et ses options — voir Modules/<...>/<Module>Module.cs.
//
// La plateforme est enregistrée en bloc ; les modules métier viendront à la suite, un appel
// par module, à mesure qu'ils sont repris de l'API Java.
builder.AddCoreModules();

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
