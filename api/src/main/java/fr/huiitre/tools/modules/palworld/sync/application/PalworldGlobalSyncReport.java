package fr.huiitre.tools.modules.palworld.sync.application;

public record PalworldGlobalSyncReport(
        PalworldSyncReport elements,
        PalworldSyncReport workSuitabilities,
        PalworldSyncReport skills,
        PalworldSyncReport pals) {}
