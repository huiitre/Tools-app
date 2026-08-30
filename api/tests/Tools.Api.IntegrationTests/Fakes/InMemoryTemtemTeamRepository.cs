using Tools.Api.Modules.Temtem.Teams.Application.Ports;
using Tools.Api.Modules.Temtem.Teams.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Application.Views;

namespace Tools.Api.IntegrationTests.Fakes;

// Équipes en mémoire. Reproduit les seules garanties que le use case délègue au SQL : le filtre
// sur le propriétaire, l'unicité du nom chez un utilisateur, et l'appartenance d'un membre à son
// équipe. Le reste — quatre techniques, six places, technique apprise — est éprouvé là où il
// vit, dans le use case et le domaine.
public sealed class InMemoryTemtemTeamRepository(InMemoryTemtemCatalogueRepository catalogue) : ITemtemTeamRepository
{
    private readonly List<Team> teams = [];
    private long nextTeamId = 1;
    private long nextMemberId = 1;

    private sealed record Member(long Id, int TemtemId, int Slot, List<int> TechniqueIds);

    private sealed class Team
    {
        public long Id { get; init; }
        public long UserId { get; init; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<Member> Members { get; } = [];
    }

    public Task<List<TemtemTeamView>> FindAllByUserId(long userId) =>
        Task.FromResult(teams
            .Where(team => team.UserId == userId)
            .OrderBy(team => team.Name)
            .Select(ToView)
            .ToList());

    public Task<TemtemTeamView?> FindByIdAndUserId(long teamId, long userId) =>
        Task.FromResult(Find(teamId, userId) is { } team ? ToView(team) : null);

    public Task<bool> ExistsForUser(long teamId, long userId) =>
        Task.FromResult(Find(teamId, userId) is not null);

    public Task<bool> NameIsTaken(long userId, string name, long? exceptTeamId = null) =>
        Task.FromResult(teams.Any(team =>
            team.UserId == userId
            && team.Id != exceptTeamId
            && string.Equals(team.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<long> Create(long userId, string name)
    {
        var team = new Team { Id = nextTeamId++, UserId = userId, Name = name };
        teams.Add(team);

        return Task.FromResult(team.Id);
    }

    public Task<bool> Rename(long teamId, long userId, string name)
    {
        if (Find(teamId, userId) is not { } team)
        {
            return Task.FromResult(false);
        }

        team.Name = name;
        team.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    public Task<bool> Delete(long teamId, long userId) =>
        Task.FromResult(Find(teamId, userId) is { } team && teams.Remove(team));

    public Task<List<int>> FindOccupiedSlots(long teamId) =>
        Task.FromResult(Find(teamId)?.Members.Select(member => member.Slot).ToList() ?? []);

    public Task<long> AddMember(long teamId, int temtemId, int slot)
    {
        var member = new Member(nextMemberId++, temtemId, slot, []);
        Find(teamId)!.Members.Add(member);

        return Task.FromResult(member.Id);
    }

    public Task ReorderMembers(long teamId, IReadOnlyList<long> memberIds)
    {
        var members = Find(teamId)!.Members;

        foreach (var (memberId, index) in memberIds.Select((memberId, index) => (memberId, index)))
        {
            var memberIndex = members.FindIndex(member => member.Id == memberId);
            members[memberIndex] = members[memberIndex] with { Slot = index + 1 };
        }

        return Task.CompletedTask;
    }

    public Task<int?> FindMemberTemtemId(long teamId, long memberId) =>
        Task.FromResult(FindMember(teamId, memberId)?.TemtemId);

    public Task<bool> DeleteMember(long teamId, long memberId)
    {
        var member = FindMember(teamId, memberId);

        return Task.FromResult(member is not null && Find(teamId)!.Members.Remove(member));
    }

    public Task ReplaceMemberTechniques(long memberId, IReadOnlyCollection<int> techniqueIds)
    {
        var member = teams.SelectMany(team => team.Members).First(candidate => candidate.Id == memberId);
        member.TechniqueIds.Clear();
        member.TechniqueIds.AddRange(techniqueIds);

        return Task.CompletedTask;
    }

    public Task TouchUpdatedAt(long teamId)
    {
        if (Find(teamId) is { } team)
        {
            team.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    private Team? Find(long teamId) => teams.FirstOrDefault(team => team.Id == teamId);

    private Team? Find(long teamId, long userId) =>
        teams.FirstOrDefault(team => team.Id == teamId && team.UserId == userId);

    private Member? FindMember(long teamId, long memberId) =>
        Find(teamId)?.Members.FirstOrDefault(member => member.Id == memberId);

    private TemtemTeamView ToView(Team team) => new(
        team.Id,
        team.Name,
        team.Members
            .OrderBy(member => member.Slot)
            .Select(member => new TemtemTeamMemberView(
                member.Id,
                member.Slot,
                catalogue.SummaryById(member.TemtemId)!,
                member.TechniqueIds.Select(catalogue.TechniqueById).ToList<TemtemTechniqueView>()))
            .ToList(),
        team.CreatedAt,
        team.UpdatedAt);
}
