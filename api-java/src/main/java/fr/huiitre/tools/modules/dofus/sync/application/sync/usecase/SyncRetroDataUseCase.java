package fr.huiitre.tools.modules.dofus.sync.application.sync.usecase;

import java.nio.file.Path;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Optional;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.mail.application.MailReport;
import fr.huiitre.tools.modules.core.mail.infrastructure.MailSenderService;
import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.report.infrastructure.ReportFileGenerator;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.dofus.game.application.view.GameVersionData;
import fr.huiitre.tools.modules.dofus.sync.application.item.SyncItemUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.itemtype.SyncItemTypesUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.monster.SyncMonsterUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.recipe.SyncRecipesUseCase;

/**
 * Orchestrateur de synchronisation pour Dofus Retro.
 * * Responsabilité :
 * - Coordonner les différents UseCases de synchro (Types, Items, Recettes, Monstres)
 * - Générer un rapport consolidé (MailReport)
 * - Gérer la sécurité et les logs
 */
@Service
@Transactional
public class SyncRetroDataUseCase implements SecuredUseCase {

    private static final Logger logger = LoggerFactory.getLogger(SyncRetroDataUseCase.class);

    private static final int INLINE_LIMIT = 100;
    private static final DateTimeFormatter TS = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

    private final MailSenderService mailSenderService;
    private final ReportFileGenerator reportFileGenerator;

    private final SyncItemTypesUseCase syncItemTypesUseCase;
    private final SyncItemUseCase syncItemUseCase;
    private final SyncRecipesUseCase syncRecipeUseCase;
    private final SyncMonsterUseCase syncMonsterUseCase;

    public SyncRetroDataUseCase(
            ReportFileGenerator reportFileGenerator,
            MailSenderService mailSenderService,
            SyncItemTypesUseCase syncItemTypesUseCase,
            SyncItemUseCase syncItemUseCase,
            SyncRecipesUseCase syncRecipeUseCase,
            SyncMonsterUseCase syncMonsterUseCase) {
        this.reportFileGenerator = reportFileGenerator;
        this.mailSenderService = mailSenderService;
        this.syncItemTypesUseCase = syncItemTypesUseCase;
        this.syncItemUseCase = syncItemUseCase;
        this.syncRecipeUseCase = syncRecipeUseCase;
        this.syncMonsterUseCase = syncMonsterUseCase;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.DOFUS);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public MailReport execute(GameVersionData gameVersion) {
        // Pas de reload de languageDataProvider ici car Retro contient ses propres labels

        // 1. Synchronisation des Types d'Items (Source : items.json clé "t")
        SyncReport itemTypeReport = syncItemTypesUseCase.execute(gameVersion);

        // 2. Synchronisation des Items (Source : items.json clé "u")
        SyncReport itemReport = syncItemUseCase.execute(gameVersion);

        // 3. Synchronisation des Recettes (Source : crafts.json)
        SyncReport recipeReport = syncRecipeUseCase.execute(gameVersion);

        // 4. Synchronisation des Monstres (Source : monsters.json)
        // SyncReport monsterReport = syncMonsterUseCase.execute(gameVersion);

        // --- Préparation du rapport ---
        List<Attachment> attachments = new ArrayList<>();
        StringBuilder body = new StringBuilder();
        
        body.append("[RETRO][SYNC] Rapport de synchronisation\n\n");
        body.append("Date              : ").append(LocalDateTime.now().format(TS)).append('\n');
        body.append("GameVersionId     : ").append(gameVersion.getId()).append('\n');
        body.append("GameVersionCode   : ").append(gameVersion.getCode()).append("\n\n");

        int globalCreated = 0;
        int globalUpdated = 0;

        // Section Types
        appendReportSection(itemTypeReport, gameVersion, body, attachments);
        globalCreated += itemTypeReport.createdCount();
        globalUpdated += itemTypeReport.updatedCount();

        // Section Items
        appendReportSection(itemReport, gameVersion, body, attachments);
        globalCreated += itemReport.createdCount();
        globalUpdated += itemReport.updatedCount();

        // Section Recettes
        appendReportSection(recipeReport, gameVersion, body, attachments);
        globalCreated += recipeReport.createdCount();
        globalUpdated += recipeReport.updatedCount();

        // Section Monstres
        /* appendReportSection(monsterReport, gameVersion, body, attachments);
        globalCreated += monsterReport.createdCount();
        globalUpdated += monsterReport.updatedCount(); */

        int total = globalCreated + globalUpdated;
        logger.info("[RETRO][SYNC] created={}, updated={}", globalCreated, globalUpdated);

        String subject = (total == 0)
                ? "[RETRO][SYNC][OK] Aucun changement"
                : "[RETRO][SYNC] +" + globalCreated + " ~" + globalUpdated;

        return new MailReport(subject, body.toString());
    }

    private void appendReportSection(
            SyncReport report,
            GameVersionData gameVersion,
            StringBuilder body,
            List<Attachment> attachments
    ) {
        body.append(report.label()).append('\n');
        body.append("Ajouts        : ").append(report.createdCount()).append('\n');
        body.append("Modifications : ").append(report.updatedCount()).append('\n');

        if (report.totalChanges() == 0) {
            body.append("Détails       : aucun changement\n\n");
        } else if (report.totalChanges() <= INLINE_LIMIT) {
            body.append('\n').append(report.toInlineDetails()).append('\n');
        } else {
            String filename = buildAttachmentFilename(gameVersion, report.code());
            Path file = reportFileGenerator.generate(
                    filename,
                    buildAttachmentHeader(gameVersion, report) + "\n" + report.toAttachmentContent()
            );
            attachments.add(new Attachment(filename, file));
            body.append("Détails       : voir pièce jointe \"")
                    .append(filename)
                    .append("\"\n\n");
        }
    }

    private String buildAttachmentFilename(GameVersionData gameVersion, String reportCode) {
        return "retro_sync_" + reportCode + "_gv" + gameVersion.getId() + ".txt";
    }

    private String buildAttachmentHeader(GameVersionData gameVersion, SyncReport report) {
        StringBuilder sb = new StringBuilder();
        sb.append("[RETRO][SYNC] Détails : ").append(report.label()).append("\n\n");
        sb.append("Date              : ").append(LocalDateTime.now().format(TS)).append('\n');
        sb.append("GameVersionId     : ").append(gameVersion.getId()).append('\n');
        sb.append("GameVersionCode   : ").append(gameVersion.getCode()).append('\n');
        sb.append("Ajouts            : ").append(report.createdCount()).append('\n');
        sb.append("Modifications     : ").append(report.updatedCount()).append('\n');
        return sb.toString();
    }

    private record Attachment(String name, Path path) {
    }
}