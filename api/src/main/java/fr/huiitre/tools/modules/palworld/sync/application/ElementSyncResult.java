package fr.huiitre.tools.modules.palworld.sync.application;

import java.util.Map;

public record ElementSyncResult(PalworldSyncReport report, Map<String, Long> idByPalElementType) {}
