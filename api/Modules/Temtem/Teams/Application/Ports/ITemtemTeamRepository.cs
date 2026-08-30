using Tools.Api.Modules.Temtem.Teams.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Ports;

// Toutes les méthodes qui désignent une équipe portent le `userId` : l'appartenance est vérifiée
// dans le use case ET dans le SQL, pour qu'aucune requête ne puisse toucher l'équipe d'un autre.
public interface ITemtemTeamRepository
{
    Task<List<TemtemTeamView>> FindAllByUserId(long userId);
    Task<TemtemTeamView?> FindByIdAndUserId(long teamId, long userId);
    Task<bool> ExistsForUser(long teamId, long userId);
    Task<bool> NameIsTaken(long userId, string name, long? exceptTeamId = null);

    Task<long> Create(long userId, string name);
    Task<bool> Rename(long teamId, long userId, string name);
    Task<bool> Delete(long teamId, long userId);

    Task<List<int>> FindOccupiedSlots(long teamId);
    Task<long> AddMember(long teamId, int temtemId, int slot);

    // Rend le Temtem du membre, ou null si ce membre n'appartient pas à cette équipe : la même
    // requête sert de contrôle d'appartenance et de source pour valider les techniques.
    Task<int?> FindMemberTemtemId(long teamId, long memberId);
    Task<bool> DeleteMember(long teamId, long memberId);
    Task ReplaceMemberTechniques(long memberId, IReadOnlyCollection<int> techniqueIds);

    // Une composition modifiée fait vieillir l'équipe, pas seulement son renommage.
    Task TouchUpdatedAt(long teamId);
}
