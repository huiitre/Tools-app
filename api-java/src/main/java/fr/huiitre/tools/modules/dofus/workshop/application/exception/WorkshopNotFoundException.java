package fr.huiitre.tools.modules.dofus.workshop.application.exception;

public class WorkshopNotFoundException extends IllegalArgumentException {
    public WorkshopNotFoundException() {
        super("L'atelier spécifié est introuvable.");
    }
}
