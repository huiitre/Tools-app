package fr.huiitre.tools.modules.palworld.tierlist.application;

import java.util.List;
import java.util.Map;

public record TierListSourceSyncData(String source, Map<String, List<TierEntrySyncData>> categories) {}
