package fr.huiitre.tools.modules.palworld.serverdata.application.view;

import java.util.UUID;

public record PlayerSummaryView(UUID playerUid, String name, Long lastOnlineRealTime) {}
