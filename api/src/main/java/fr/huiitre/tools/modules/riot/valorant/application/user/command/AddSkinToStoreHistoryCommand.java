package fr.huiitre.tools.modules.riot.valorant.application.user.command;

import java.time.LocalDate;
import java.util.List;

public class AddSkinToStoreHistoryCommand {
    private List<Long> skinIds;
    private LocalDate seenAt;

    public AddSkinToStoreHistoryCommand() {}

    public AddSkinToStoreHistoryCommand(List<Long> skinIds, LocalDate seenAt) {
        this.skinIds = skinIds;
        this.seenAt = seenAt;
    }

    public List<Long> getSkinIds() {
        return skinIds;
    }

    public void setSkinIds(List<Long> skinIds) {
        this.skinIds = skinIds;
    }

    public LocalDate getSeenAt() {
        return seenAt;
    }

    public void setSeenAt(LocalDate seenAt) {
        this.seenAt = seenAt;
    }
}
