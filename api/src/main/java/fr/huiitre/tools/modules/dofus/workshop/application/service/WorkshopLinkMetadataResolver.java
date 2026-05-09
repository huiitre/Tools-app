package fr.huiitre.tools.modules.dofus.workshop.application.service;

import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

@Service
public class WorkshopLinkMetadataResolver {

    private final Map<LinkSource, LinkSourceHandler> handlers;

    public WorkshopLinkMetadataResolver(List<LinkSourceHandler> handlers) {
        this.handlers = handlers.stream()
            .collect(Collectors.toMap(LinkSourceHandler::source, h -> h));
    }

    public String validateAndResolveLabel(LinkSource source, String url) {
        LinkSourceHandler handler = handlers.get(source);
        if (handler == null) {
            throw new IllegalStateException("Aucun handler configuré pour la source : " + source);
        }
        return handler.validateAndResolveLabel(url);
    }

    public void validate(LinkSource source, String url) {
        LinkSourceHandler handler = handlers.get(source);
        if (handler == null) {
            throw new IllegalStateException("Aucun handler configuré pour la source : " + source);
        }
        handler.validate(url);
    }
}
