using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Les paramètres qui existent. C'est **la** source de vérité : une ligne de
// `tools_core.setting_value` dont le code n'apparaît pas ici est ignorée, jamais une erreur.
//
// Le catalogue est en dur, et pas en base, pour une raison simple : un paramètre n'existe que
// parce qu'un bout de code le lit. En créer un depuis une interface donnerait une ligne que
// rien ne consomme ; en supprimer une laisserait le code qui la lit retomber en silence sur son
// défaut. Ici, supprimer une définition fait échouer la compilation à l'endroit qui s'en
// servait. Ajouter un paramètre = un commit, jamais une migration.
public static class SettingCatalog
{
    // Les définitions sont des champs **nommés**, pas des entrées anonymes dans une liste.
    // C'est ce qui permet à un use case d'écrire `settings.Get(SettingCatalog.Ui.CompactMode)`
    // sans jamais citer le code du paramètre : pas de faute de frappe possible, le type de la
    // valeur est connu à la compilation, et supprimer une définition casse le build à l'endroit
    // qui la lisait — au lieu de retomber en silence sur un défaut.

    public static class Ui
    {
        public static readonly ChoiceSetting Theme = new()
        {
            Code = "ui.theme",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Options = ["light", "dark"],
            Default = "dark"
        };

        public static readonly BooleanSetting CompactMode = new()
        {
            Code = "ui.compactMode",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.ReadOnly,
            Default = false
        };

        public static readonly IntegerSetting PageSize = new()
        {
            Code = "ui.pageSize",
            AllowedScopes = SettingScopes.All,
            MinRoleToView = RoleCode.ReadOnly,
            MinRoleToSetOwn = RoleCode.User,
            Min = 10,
            Max = 200,
            Default = 25
        };
    }

    // Paramètres d'instance : ils ne déclarent que `Global`. « Mon mode maintenance à moi » n'a
    // aucun sens — ce n'est pas une question de droit, la notion n'existe pas.
    public static class Instance
    {
        public static readonly BooleanSetting MaintenanceMode = new()
        {
            Code = "instance.maintenanceMode",
            AllowedScopes = SettingScopes.GlobalOnly,
            MinRoleToView = RoleCode.Admin,
            Default = false
        };
    }

    // Le recensement. Toute définition déclarée au-dessus doit figurer ici, sinon elle n'est ni
    // servie au frontend ni lisible en base — un test le vérifie par réflexion.
    public static readonly IReadOnlyList<SettingDefinition> All =
    [
        Ui.Theme,
        Ui.CompactMode,
        Ui.PageSize,
        Instance.MaintenanceMode
    ];

    // Index par code, codes historiques compris : une valeur écrite sous un ancien code reste
    // lue par la définition qui l'a remplacé.
    private static readonly IReadOnlyDictionary<string, SettingDefinition> ByCode = BuildIndex();

    public static SettingDefinition? Find(string code) =>
        ByCode.TryGetValue(code, out var definition) ? definition : null;

    // Tous les codes à interroger en base pour couvrir le catalogue, anciens codes inclus.
    public static IReadOnlyList<string> AllStoredCodes { get; } = [.. ByCode.Keys];

    // Les incohérences se constatent au démarrage, pas en production. Un catalogue invalide
    // empêche l'application de démarrer : c'est une erreur de programmation, elle doit se voir
    // au premier lancement et non le jour où quelqu'un ouvre ses réglages.
    private static Dictionary<string, SettingDefinition> BuildIndex()
    {
        var index = new Dictionary<string, SettingDefinition>(StringComparer.Ordinal);

        foreach (var definition in All)
        {
            if (definition.AllowedScopes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : aucune portée autorisée, il ne pourrait jamais recevoir de valeur.");
            }

            var acceptsUser = definition.AllowedScopes.Contains(SettingScope.User);

            if (acceptsUser && definition.MinRoleToSetOwn is null)
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : la portée User est autorisée mais MinRoleToSetOwn n'est pas déclaré.");
            }

            if (!acceptsUser && definition.MinRoleToSetOwn is not null)
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : MinRoleToSetOwn est déclaré alors que la portée User n'est pas autorisée.");
            }

            // On ne peut pas modifier ce qu'on ne voit pas : l'inverse produirait un paramètre
            // réglable en aveugle.
            if (definition.MinRoleToSetOwn is { } setOwn && !setOwn.HasAtLeast(definition.MinRoleToView))
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : MinRoleToSetOwn ({setOwn}) est sous MinRoleToView ({definition.MinRoleToView}).");
            }

            if (!definition.MinRoleToAdminister.HasAtLeast(definition.MinRoleToView))
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : MinRoleToAdminister ({definition.MinRoleToAdminister}) est sous MinRoleToView ({definition.MinRoleToView}).");
            }

            // Le défaut du catalogue doit satisfaire ses propres contraintes, sinon la valeur
            // de repli est elle-même invalide et le paramètre n'a aucune valeur sûre.
            if (!definition.Accepts(definition.DefaultValue))
            {
                throw new InvalidOperationException(
                    $"Paramètre '{definition.Code}' : la valeur par défaut ne satisfait pas ses propres contraintes.");
            }

            foreach (var code in definition.AllCodes)
            {
                // Deux définitions sur un même code, c'est un paramètre qui en écrase un autre
                // sans que rien ne le signale.
                if (!index.TryAdd(code, definition))
                {
                    throw new InvalidOperationException(
                        $"Code de paramètre en double : '{code}'.");
                }
            }
        }

        return index;
    }
}
