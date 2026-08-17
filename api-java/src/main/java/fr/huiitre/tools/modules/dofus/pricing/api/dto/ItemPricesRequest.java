package fr.huiitre.tools.modules.dofus.pricing.api.dto;

import java.util.List;

public record ItemPricesRequest(List<Long> itemIds) {}