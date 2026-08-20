using Tools.Api.Modules.Core.Settings.Application.Ports;
using Tools.Api.Modules.Core.Settings.Application.Services;
using Tools.Api.Modules.Core.Settings.Domain;
using Tools.Api.Modules.Core.Settings.Infrastructure;

namespace Tools.Api.Modules.Core.Settings;

// Composition du module Settings : les paramètres de l'application, leurs valeurs et leur
// résolution.
//
// Le catalogue n'est pas enregistré dans le conteneur : c'est un type statique, sans état ni
// dépendance. Le toucher au démarrage n'est donc pas une injection mais une **vérification** —
// son garde-fou s'exécute là, et une définition incohérente empêche l'application de démarrer
// au lieu d'échouer le jour où quelqu'un ouvre ses réglages.
public static class SettingsModule
{
    public static IHostApplicationBuilder AddSettingsModule(this IHostApplicationBuilder builder)
    {
        _ = SettingCatalog.All;

        builder.Services.AddScoped<ISettingValueRepository, PostgresSettingValueRepository>();
        builder.Services.AddScoped<SettingReader>();

        return builder;
    }
}
