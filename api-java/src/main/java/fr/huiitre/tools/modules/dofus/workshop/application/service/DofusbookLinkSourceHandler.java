package fr.huiitre.tools.modules.dofus.workshop.application.service;

import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.springframework.stereotype.Service;

import fr.huiitre.tools.modules.dofus.workshop.application.exception.InvalidWorkshopLinkException;
import fr.huiitre.tools.modules.dofus.workshop.domain.LinkSource;

@Service
public class DofusbookLinkSourceHandler implements LinkSourceHandler {

    // https://d-bk.net/fr/d/1VO4I
    private static final Pattern SHORT_LINK = Pattern.compile(
        "^https?://(?:www\\.)?d-bk\\.net/fr/d/([A-Za-z0-9]+)/?$"
    );

    // https://www.dofusbook.net/fr/equipement/22257026-db/objets
    private static final Pattern PUBLIC_LINK = Pattern.compile(
        "^https?://(?:www\\.)?dofusbook\\.net/fr/equipement/(\\d+)-([^/]+)/objets/?$"
    );

    // https://www.dofusbook.net/fr/equipement/private/22257026-terre-199-clem-nevarrk/objets
    private static final Pattern PRIVATE_LINK = Pattern.compile(
        "^https?://(?:www\\.)?dofusbook\\.net/fr/equipement/private/(\\d+)-([^/]+)/objets/?$"
    );

    @Override
    public LinkSource source() {
        return LinkSource.DOFUSBOOK;
    }

    @Override
    public String validateAndResolveLabel(String url) {
        Matcher shortMatcher = SHORT_LINK.matcher(url);
        if (shortMatcher.matches()) {
            return "Dofus Book " + shortMatcher.group(1);
        }

        Matcher privateMatcher = PRIVATE_LINK.matcher(url);
        if (privateMatcher.matches()) {
            String slug = privateMatcher.group(2).replace("-", " ");
            return "Dofus Book " + slug;
        }

        Matcher publicMatcher = PUBLIC_LINK.matcher(url);
        if (publicMatcher.matches()) {
            String id = publicMatcher.group(1);
            String slug = publicMatcher.group(2);
            return "db".equalsIgnoreCase(slug)
                ? "Dofus Book " + id
                : "Dofus Book " + slug.replace("-", " ");
        }

        throw new InvalidWorkshopLinkException(
            "L'URL n'est pas un lien Dofusbook valide. Formats acceptés : " +
            "d-bk.net/fr/d/{code}, " +
            "dofusbook.net/fr/equipement/{id}-{slug}/objets, " +
            "dofusbook.net/fr/equipement/private/{id}-{slug}/objets"
        );
    }
}
