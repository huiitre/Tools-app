using Tools.Api.Modules.Core.AppLogs.Application;
using Tools.Api.Modules.Core.AppLogs.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Double de test du journal : les routes d'intégration ne doivent jamais dépendre de PostgreSQL
// pour vérifier l'événement qu'elles viennent de produire.
public sealed class RecordingAppLogRepository : IAppLogRepository
{
    private readonly List<AppLogEntry> entries = [];

    public IReadOnlyList<AppLogEntry> Entries => entries;

    public Task<long> InsertAsync(AppLogEntry entry)
    {
        entries.Add(entry);
        return Task.FromResult((long)entries.Count);
    }

    public Task<AppLogPageDto> FindForAdminAsync(AppLogListQuery query) =>
        Task.FromResult(new AppLogPageDto([], 0, query.Page, query.PageSize));

    public void Reset() => entries.Clear();
}
