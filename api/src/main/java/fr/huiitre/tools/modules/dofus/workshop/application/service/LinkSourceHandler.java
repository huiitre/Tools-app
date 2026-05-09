package fr.huiitre.tools.modules.dofus.workshop.application.service;

import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

public interface LinkSourceHandler {
    LinkSource source();
    String validateAndResolveLabel(String url);

    default void validate(String url) {
        validateAndResolveLabel(url);
    }
}
