using Tools.Api.Modules.Core.Access;
using Tools.Api.Modules.Core.Admin;
using Tools.Api.Modules.Core.Auth;
using Tools.Api.Modules.Core.Common;
using Tools.Api.Modules.Core.GameServers;
using Tools.Api.Modules.Core.Health;
using Tools.Api.Modules.Core.Mail;
using Tools.Api.Modules.Core.Notifications;
using Tools.Api.Modules.Core.Realtime;
using Tools.Api.Modules.Core.Security;
using Tools.Api.Modules.Core.Settings;
using Tools.Api.Modules.Core.Users;

namespace Tools.Api.Modules.Core;

// Composition de la plateforme : les modules transverses, ceux dont tout le reste dépend et
// qui ne dépendent d'aucun métier.
//
// Ce fichier existe pour que `Program.cs` reste lisible à mesure que les modules métier
// arrivent depuis l'API Java. Sans lui, la racine de composition mélangerait sur une même
// liste des briques de plateforme et des modules fonctionnels, alors que les deux n'ont ni le
// même cycle de vie ni le même sens : Dofus peut disparaître, Security non.
//
// Il n'ajoute aucune indirection : c'est le même enchaînement d'extensions qu'avant, déplacé
// d'un cran. Chaque module garde son `<Module>Module.cs` et reste enregistrable seul.
public static class CoreModule
{
    public static IHostApplicationBuilder AddCoreModules(this IHostApplicationBuilder builder)
    {
        // Common vient en premier : les autres dépendent de son contrat d'erreur et de son
        // accès PostgreSQL, jamais l'inverse.
        return builder.AddCommonModule()
            .AddSecurityModule()
            .AddAuthModule()
            .AddMailModule()
            .AddRealtimeModule()
            .AddNotificationsModule()
            .AddUsersModule()
            .AddSettingsModule()
            .AddAccessModule()
            .AddAdminModule()
            .AddHealthModule()
            .AddGameServersModule();
    }
}
