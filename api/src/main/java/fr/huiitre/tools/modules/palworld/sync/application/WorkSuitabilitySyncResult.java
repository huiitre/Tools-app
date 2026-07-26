package fr.huiitre.tools.modules.palworld.sync.application;

import java.util.Map;

public record WorkSuitabilitySyncResult(PalworldSyncReport report, Map<String, Long> idBySlug) {}
