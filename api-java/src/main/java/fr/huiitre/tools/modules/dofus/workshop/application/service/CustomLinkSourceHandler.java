package fr.huiitre.tools.modules.dofus.workshop.application.service;

import java.net.URI;
import java.net.URISyntaxException;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.dofus.workshop.application.exception.InvalidWorkshopLinkException;
import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

@Service
public class CustomLinkSourceHandler implements LinkSourceHandler {

    @Override
    public LinkSource source() {
        return LinkSource.CUSTOM;
    }

    @Override
    public String validateAndResolveLabel(String url) {
        try {
            URI uri = new URI(url.trim());
            String scheme = uri.getScheme();
            if (!"http".equalsIgnoreCase(scheme) && !"https".equalsIgnoreCase(scheme)) {
                throw new InvalidWorkshopLinkException("Seuls les liens http et https sont autorisés.");
            }
            if (uri.getHost() == null || uri.getHost().isEmpty()) {
                throw new InvalidWorkshopLinkException("L'URL n'est pas valide : hôte manquant.");
            }
        } catch (URISyntaxException e) {
            throw new InvalidWorkshopLinkException("L'URL n'est pas valide.");
        }
        return url;
    }
}
