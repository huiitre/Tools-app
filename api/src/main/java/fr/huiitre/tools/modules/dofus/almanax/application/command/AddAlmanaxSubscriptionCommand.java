package fr.huiitre.tools.modules.dofus.almanax.application.command;

import java.time.LocalDate;

public class AddAlmanaxSubscriptionCommand {

    private Long almanaxId;
    private LocalDate date;

    public Long getAlmanaxId() {
        return almanaxId;
    }

    public LocalDate getDate() {
        return date;
    }
}
