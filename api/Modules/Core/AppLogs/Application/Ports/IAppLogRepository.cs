namespace Tools.Api.Modules.Core.AppLogs.Application.Ports;

public interface IAppLogRepository
{
    // Le port est volontairement append-only : aucune opération de modification ou suppression.
    Task<long> InsertAsync(AppLogEntry entry);

    Task<AppLogPageDto> FindForAdminAsync(AppLogListQuery query);
}
