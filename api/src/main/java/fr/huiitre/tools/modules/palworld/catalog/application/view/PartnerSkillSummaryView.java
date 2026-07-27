package fr.huiitre.tools.modules.palworld.catalog.application.view;

import java.util.List;

public record PartnerSkillSummaryView(
        String title,
        String description,
        String iconUrl,
        List<PartnerSkillRankSummaryView> ranks) {}
