using Tools.Api.Modules.Temtem.Creatures.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Application.Ports;

// Le catalogue répond aux questions sur le jeu — y compris celles que se posent les équipes
// avant d'accepter une composition.
public interface ITemtemCreatureRepository
{
    Task<List<TemtemSummaryView>> FindAll();
    Task<TemtemDetailView?> FindBySlug(string slug);
    Task<bool> Exists(int temtemId);

    // Ce que ce Temtem sait apprendre, tous moyens confondus. Aucune clé étrangère ne dit qu'une
    // technique est à la portée d'un Temtem donné : c'est temtem_technique qui le dit.
    Task<HashSet<int>> FindLearnedTechniqueIds(int temtemId);
}
