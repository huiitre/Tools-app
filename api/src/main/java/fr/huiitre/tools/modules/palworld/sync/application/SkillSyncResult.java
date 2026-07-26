package fr.huiitre.tools.modules.palworld.sync.application;

import java.util.Map;

public record SkillSyncResult(PalworldSyncReport report, Map<String, Long> idBySlug) {}
