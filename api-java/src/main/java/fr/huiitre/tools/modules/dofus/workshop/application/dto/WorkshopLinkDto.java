package fr.huiitre.tools.modules.dofus.workshop.application.dto;

import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

public record WorkshopLinkDto(
    Long id,
    LinkSource source,
    String url,
    String label
) {}
