using Tools.Api.Modules.Temtem.Sync.Application.Data;

namespace Tools.Api.Modules.Temtem.Sync.Application.Ports;

// Ce que l'écriture d'une ligne a réellement changé. « Inchangé » n'est pas du bruit : c'est le
// cas courant d'une synchronisation horaire, et le distinguer évite de gonfler le rapport.
public enum TemtemUpsertOutcome
{
    Unchanged,
    Created,
    Updated
}

// Écriture du catalogue. Toutes les méthodes s'exécutent dans la transaction ouverte par le use
// case : la synchronisation est indivisible.
public interface ITemtemCatalogueRepository
{
    Task<TemtemUpsertOutcome> UpsertCategory(TemtemCategoryData data);
    Task<int> DeleteCategoriesExcept(IReadOnlyCollection<string> codes);

    Task<TemtemUpsertOutcome> UpsertPriority(TemtemPriorityData data);
    Task<int> DeletePrioritiesExcept(IReadOnlyCollection<int> orders);

    Task<TemtemUpsertOutcome> UpsertType(TemtemTypeData data);
    Task<int> DeleteTypesExcept(IReadOnlyCollection<int> ids);

    Task<TemtemUpsertOutcome> UpsertCreature(TemtemCreatureData data);
    Task<int> DeleteCreaturesExcept(IReadOnlyCollection<int> ids);

    Task<TemtemUpsertOutcome> UpsertTechnique(TemtemTechniqueData data);
    Task<int> DeleteTechniquesExcept(IReadOnlyCollection<int> ids);

    Task<TemtemUpsertOutcome> UpsertTrait(TemtemTraitData data);
    Task<int> DeleteTraitsExcept(IReadOnlyCollection<int> ids);

    // Réécritures en bloc : purge puis insertion, dans la même transaction.
    Task<int> ReplaceTechniqueTargets(IReadOnlyCollection<TemtemTechniqueData> techniques);
    Task<int> ReplaceLearnings(IReadOnlyCollection<TemtemLearningData> learnings);
    Task<int> ReplaceTraitLinks(IReadOnlyCollection<TemtemTraitLinkData> links);
    Task<int> ReplaceTypeMatchups(IReadOnlyCollection<TemtemTypeMatchupData> matchups);
}
