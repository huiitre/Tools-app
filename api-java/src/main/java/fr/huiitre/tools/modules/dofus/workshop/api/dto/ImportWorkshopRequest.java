package fr.huiitre.tools.modules.dofus.workshop.api.dto;

import java.util.List;

import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

public record ImportWorkshopRequest(
    String name,
    List<Long> breedIds,
    List<Long> jobIds,
    List<Long> tagIds,
    List<Link> links
) {
    //* Libellé facultatif : sans lui, celui déduit de l'URL (comme à l'ajout d'un lien).
    public record Link(LinkSource source, String url, String label) {}
}
