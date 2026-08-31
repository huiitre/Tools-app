using Tools.Api.Modules.Core.Common.Application.Ports;

namespace Tools.Api.Modules.Riot.Sync.Application.Usecases;

// Synchronisation complète du catalogue Valorant depuis les assets locaux.
// L'ordre n'est pas négociable : les armes avant les skins, qui s'y rattachent, et les raretés
// avant tout le reste. Le tout dans une seule transaction — une passe interrompue au milieu
// laisserait des skins orphelins de leur arme.
public sealed class SyncValorantUseCase(
    SyncValorantContentTiersUseCase syncContentTiersUseCase,
    SyncValorantWeaponsUseCase syncWeaponsUseCase,
    SyncValorantSkinsUseCase syncSkinsUseCase,
    SyncValorantBundlesUseCase syncBundlesUseCase,
    ITransactionManager transactionManager
)
{
    public async Task<ValorantGlobalSyncReport> Execute()
    {
        await using var transaction = await transactionManager.BeginAsync();

        var contentTiers = await syncContentTiersUseCase.Execute();
        var weapons = await syncWeaponsUseCase.Execute();
        var skins = await syncSkinsUseCase.Execute(weapons.WeaponAssetIdToDbId);
        var bundles = await syncBundlesUseCase.Execute();

        await transaction.CommitAsync();

        return new ValorantGlobalSyncReport(contentTiers, weapons.Report, skins, bundles);
    }
}
