using Tools.Api.Modules.Realtime.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

public sealed class InMemoryRecipientResolver : IRecipientResolver
{
    public const long ModuleMemberUserId = 55;

    public IReadOnlyList<string> RoleCodesAsked { get; private set; } = [];

    public long? ModuleIdAsked { get; private set; }

    public void Clear()
    {
        RoleCodesAsked = [];
        ModuleIdAsked = null;
    }

    public Task<bool> UserExistsAsync(long userId) => Task.FromResult(true);

    public Task<IReadOnlyList<long>> FindByRoleCodesAsync(IReadOnlyCollection<string> roleCodes)
    {
        RoleCodesAsked = roleCodes.ToList();
        return Task.FromResult<IReadOnlyList<long>>([ModuleMemberUserId]);
    }

    public Task<IReadOnlyList<long>> FindByModuleIdAsync(long moduleId)
    {
        ModuleIdAsked = moduleId;
        return Task.FromResult<IReadOnlyList<long>>([ModuleMemberUserId]);
    }
}
