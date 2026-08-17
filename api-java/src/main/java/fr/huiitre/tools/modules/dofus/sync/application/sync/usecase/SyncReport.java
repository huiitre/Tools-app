package fr.huiitre.tools.modules.dofus.sync.application.sync.usecase;

import java.util.List;

public record SyncReport(
        String code,
        String label,
        List<String> created,
        List<String> updated) {

    public int createdCount() {
        return created.size();
    }

    public int updatedCount() {
        return updated.size();
    }

    public int totalChanges() {
        return created.size() + updated.size();
    }

    public String toInlineDetails() {
        StringBuilder sb = new StringBuilder();

        if (created.isEmpty()) {
            sb.append("AJOUTS: aucun\n");
        } else {
            sb.append("AJOUTS:\n");
            created.forEach(l -> sb.append(" + ").append(l).append('\n'));
        }

        sb.append('\n');

        if (updated.isEmpty()) {
            sb.append("MODIFICATIONS: aucune\n");
        } else {
            sb.append("MODIFICATIONS:\n");
            updated.forEach(l -> sb.append(" ~ ").append(l).append('\n'));
        }

        return sb.toString();
    }

    public String toAttachmentContent() {
        return toInlineDetails();
    }
}
