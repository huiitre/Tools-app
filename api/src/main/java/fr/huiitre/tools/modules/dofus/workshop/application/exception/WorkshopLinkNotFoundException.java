package fr.huiitre.tools.modules.dofus.workshop.application.exception;

public class WorkshopLinkNotFoundException extends IllegalArgumentException {
    public WorkshopLinkNotFoundException() {
        super("Le lien spécifié est introuvable.");
    }
}
