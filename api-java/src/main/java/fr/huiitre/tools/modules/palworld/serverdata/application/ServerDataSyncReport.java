package fr.huiitre.tools.modules.palworld.serverdata.application;

public record ServerDataSyncReport(int filesImported, int filesAlreadyImportedButNotMoved, int filesFailed) {}
