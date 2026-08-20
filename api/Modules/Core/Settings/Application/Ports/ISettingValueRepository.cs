using Tools.Api.Modules.Core.Settings.Domain;

namespace Tools.Api.Modules.Core.Settings.Application.Ports;

// Accès aux valeurs stockées. Ce port ne décide de rien : il ramène des lignes, la règle vit
// dans `SettingResolution`. Une requête SQL qui trancherait la priorité ne serait vérifiable
// qu'avec une base, et se dupliquerait au premier autre appelant.
public interface ISettingValueRepository
{
    // Toutes les lignes susceptibles de concerner cet appelant, pour les codes demandés.
    //
    // Le filtrage est volontairement **large** : on ramène les lignes de tous les rôles cités,
    // sans savoir lequel s'applique à quel paramètre — un paramètre de module se juge sur le
    // rôle du module, un paramètre transverse sur le rôle global, et une seule requête ne peut
    // pas faire les deux. `SettingResolution` refait le tri exact ensuite, c'est lui qui fait
    // foi. Le surcoût est de quelques lignes.
    //
    // `userId` nul et `roleCodes` vide — le cas d'une tâche de fond — ne ramènent que les
    // lignes globales.
    Task<IReadOnlyList<SettingValue>> FindAsync(
        IReadOnlyCollection<string> codes,
        long? userId,
        IReadOnlyCollection<string> roleCodes);
}
