package fr.huiitre.tools.modules.palworld.sync.application;

public record PassiveSkillSyncData(
        String id,
        String name,
        String description,
        int rank,
        boolean negative,
        boolean worldTree,
        String rankIconUrl,
        String rawPayloadJson) {}
