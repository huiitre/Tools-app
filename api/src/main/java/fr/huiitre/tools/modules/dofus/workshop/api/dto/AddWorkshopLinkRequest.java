package fr.huiitre.tools.modules.dofus.workshop.api.dto;

import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

public record AddWorkshopLinkRequest(
    LinkSource source,
    String url
) {}
