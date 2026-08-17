package fr.huiitre.tools.modules.riot.sync.application;

import java.util.Map;
import java.util.UUID;

public record ValorantWeaponSyncResult(ValorantSyncReport report, Map<UUID, Long> weaponAssetIdToDbId) {}
