using Tools.ApiCore.Modules.Access.Application.Dto;

namespace Tools.ApiCore.Modules.Access.Application.Ports;

// Catalogue des modules fonctionnels.
public interface IModuleRepository
{
    Task<IReadOnlyList<ModuleDto>> FindAllAsync();

    Task<bool> ExistsAsync(long moduleId);

    // Vrai si un module porte déjà ce code, en excluant éventuellement celui qu'on modifie.
    // Le code est unique en base : le vérifier avant permet de rendre une erreur lisible
    // plutôt qu'une violation de contrainte.
    Task<bool> CodeExistsAsync(string code, long? excludedModuleId = null);

    Task<long> CreateAsync(string code, string name, string? description);

    Task UpdateAsync(long moduleId, string code, string name, string? description, bool active);
}
