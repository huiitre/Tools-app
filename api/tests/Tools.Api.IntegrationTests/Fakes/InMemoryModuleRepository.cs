using Tools.Api.Modules.Core.Access.Application.Dto;
using Tools.Api.Modules.Core.Access.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Catalogue des modules fonctionnels en mémoire. Seul le module 1 existe.
public sealed class InMemoryModuleRepository : IModuleRepository
{
    public const long ExistingModuleId = 1;
    public const string ExistingModuleCode = "dofus";

    private readonly List<ModuleDto> modules =
    [
        new(ExistingModuleId, ExistingModuleCode, "Dofus", null, true, DateTime.UtcNow, DateTime.UtcNow)
    ];

    public Task<IReadOnlyList<ModuleDto>> FindAllAsync() =>
        Task.FromResult<IReadOnlyList<ModuleDto>>(modules);

    public Task<bool> ExistsAsync(long moduleId) =>
        Task.FromResult(modules.Any(module => module.Id == moduleId));

    public Task<bool> CodeExistsAsync(string code, long? excludedModuleId = null) =>
        Task.FromResult(modules.Any(module =>
            module.Code == code && (excludedModuleId is null || module.Id != excludedModuleId)));

    public Task<long> CreateAsync(string code, string name, string? description)
    {
        var id = modules.Count + 1;
        modules.Add(new ModuleDto(id, code, name, description, false, DateTime.UtcNow, DateTime.UtcNow));
        return Task.FromResult((long)id);
    }

    public Task UpdateAsync(long moduleId, string code, string name, string? description, bool active)
    {
        var index = modules.FindIndex(module => module.Id == moduleId);
        if (index >= 0)
        {
            modules[index] = modules[index] with
            {
                Code = code,
                Name = name,
                Description = description,
                Active = active
            };
        }

        return Task.CompletedTask;
    }
}
