using System.Text.Json.Nodes;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Settings.Domain;

// Ce qu'un paramètre **est** : son code, son type, ses contraintes, qui le voit et qui le règle.
//
// Le catalogue vit dans le code et non en base — voir `SettingCatalog`. La base ne retient que
// les valeurs posées. Ajouter un paramètre est donc un commit, jamais une migration.
//
// Les quatre droits ci-dessous répondent à quatre questions distinctes, et les écraser en une
// seule ne marche pas : « tout le monde peut choisir son thème » et « tout le monde peut fixer
// le thème du site » ne sont pas la même phrase.
public abstract record SettingDefinition
{
    // Identifiant stable du paramètre, unique dans tout le catalogue. Par convention
    // `<namespace>.<nom>`, le namespace valant le code du module pour un paramètre de module
    // (`dofus.autoSync`) et un domaine fonctionnel sinon (`ui.theme`).
    //
    // **C'est un nom, pas un lien.** La correspondance avec `Module` n'est pas vérifiée, et
    // c'est délibéré : renommer un `ModuleCode` ne doit rien coûter aux paramètres. Un
    // paramètre déplacé de module garde son code — il devient historique, comme n'importe
    // quelle constante qu'on ne renomme pas parce que ça ne rapporterait rien.
    public required string Code { get; init; }

    // Module de rattachement, ou null pour un paramètre transverse. Détermine quel rôle est
    // comparé aux quatre seuils ci-dessous : celui du module, ou le rôle global.
    public ModuleCode? Module { get; init; }

    // Accroches que ce paramètre accepte, parmi les trois. Un paramètre d'instance ne déclare
    // que `Global` : la notion de valeur par utilisateur n'existe pas pour lui, et l'exprimer
    // par un rôle très élevé serait faux — ce n'est pas une question de droit.
    public required IReadOnlySet<SettingScope> AllowedScopes { get; init; }

    // Rôle minimum pour **voir** le paramètre. C'est un seuil : `Moderator` le rend visible aux
    // modérateurs et à tout ce qui est au-dessus, administrateurs compris.
    public required RoleCode MinRoleToView { get; init; }

    // Rôle minimum pour poser **sa propre** valeur. Null quand `User` n'est pas dans
    // `AllowedScopes` — il n'y a alors rien à autoriser.
    public RoleCode? MinRoleToSetOwn { get; init; }

    // Rôle minimum pour poser une valeur **globale ou par rôle**. C'est de l'administration :
    // régler son thème et fixer celui du site n'ont pas le même public.
    public RoleCode MinRoleToAdminister { get; init; } = RoleCode.Admin;

    // Codes qu'a portés ce paramètre par le passé. Lus en repli, jamais écrits : renommer un
    // paramètre est un changement de code, pas une migration SQL.
    public IReadOnlyList<string> PreviousCodes { get; init; } = [];

    // Type de valeur, tel que le frontend l'utilise pour choisir son composant de saisie.
    public abstract string Kind { get; }

    // Valeur retenue quand aucune n'a été posée à aucune accroche.
    public abstract JsonNode DefaultValue { get; }

    // Contraintes du paramètre, envoyées au frontend avec la définition pour qu'il borne ses
    // champs sans réimplémenter la règle. Le refus reste celui du serveur.
    public abstract IReadOnlyDictionary<string, object?> Constraints { get; }

    // Une valeur est-elle acceptable pour ce paramètre ?
    //
    // Appelé à l'écriture, mais **aussi à la lecture** : une contrainte resserrée après coup
    // rend invalide une valeur déjà en base. La résolution l'écarte alors et retombe sur
    // l'héritage, plutôt que de faire échouer la page de réglages de tout le monde.
    public abstract bool Accepts(JsonNode? value);

    // Tous les codes sous lesquels ce paramètre peut avoir été stocké.
    public IEnumerable<string> AllCodes => PreviousCodes.Prepend(Code);
}

// Un paramètre dont on connaît le type C# de la valeur.
//
// C'est ce qui rend la lecture typée sans jamais nommer un type à l'appel :
// `await settings.Get(SettingCatalog.Ui.CompactMode)` rend un `bool`, et rendrait une erreur de
// compilation si on l'affectait à autre chose. Le code d'un paramètre n'apparaît nulle part
// chez l'appelant — donc aucune faute de frappe possible, et supprimer une définition casse la
// compilation à l'endroit qui s'en servait.
public abstract record SettingDefinition<TValue> : SettingDefinition
{
    // Valeur retenue quand rien n'a été posé. Déclarée dans le type de la valeur, pas en JSON :
    // une valeur par défaut incohérente ne doit pas pouvoir s'écrire.
    public required TValue Default { get; init; }

    // Traduit une valeur stockée vers son type C#. N'est appelée que sur une valeur déjà
    // validée par `Accepts` — la résolution écarte les autres.
    public abstract TValue Read(JsonNode value);
}
