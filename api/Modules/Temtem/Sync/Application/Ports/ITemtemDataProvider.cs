using Tools.Api.Modules.Temtem.Sync.Application.Data;

namespace Tools.Api.Modules.Temtem.Sync.Application.Ports;

// Une seule source — le dossier publié par l'extracteur — donc un seul port. Le découper par
// fichier ne donnerait que des interfaces à une méthode, toutes servies par le même adaptateur.
public interface ITemtemDataProvider
{
    Task<List<TemtemCategoryData>> FetchCategories();
    Task<List<TemtemPriorityData>> FetchPriorities();
    Task<List<TemtemTypeData>> FetchTypes();
    Task<List<TemtemCreatureData>> FetchCreatures();
    Task<List<TemtemTechniqueData>> FetchTechniques();
    Task<List<TemtemTraitData>> FetchTraits();
    Task<List<TemtemLearningData>> FetchLearnings();
    Task<List<TemtemTraitLinkData>> FetchTraitLinks();
    Task<List<TemtemTypeMatchupData>> FetchTypeMatchups();
}
