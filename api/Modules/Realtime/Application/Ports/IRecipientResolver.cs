namespace Tools.ApiCore.Modules.Realtime.Application.Ports;

public interface IRecipientResolver
{
    Task<bool> UserExistsAsync(long userId);

    // Hors comptes TECH.
    Task<IReadOnlyList<long>> FindByRoleCodesAsync(IReadOnlyCollection<string> roleCodes);

    // Membres du module désigné, hors comptes TECH.
    Task<IReadOnlyList<long>> FindByModuleIdAsync(long moduleId);
}
