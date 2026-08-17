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
import fr.huiitre.tools.modules.dofus.sync.application.almanax.SyncAlmanaxUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.area.SyncAreaUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.item.SyncItemUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.itemtype.SyncItemTypesUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.monster.SyncMonsterUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.recipe.SyncRecipesUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.subarea.SyncSubareaUseCase;
import fr.huiitre.tools.modules.dofus.sync.application.sync.ports.Dofus3LanguageDataProvider;

@Service
@Transactional
public class SyncDofus3DataUseCase implements SecuredUseCase {

    private static final Logger logger = LoggerFactory.getLogger(SyncDofus3DataUseCase.class);

    private static final int INLINE_LIMIT = 100;
    private static final DateTimeFormatter TS = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

    private final Dofus3LanguageDataProvider languageDataProvider;

    private final MailSenderService mailSenderService;
    private final ReportFileGenerator reportFileGenerator;

    private final SyncItemTypesUseCase syncItemTypesUseCase;
    private final SyncItemUseCase syncItemUseCase;
    private final SyncAlmanaxUseCase syncAlmanaxUseCase;
    private final SyncRecipesUseCase syncRecipeUseCase;
    private final SyncAreaUseCase syncAreaUseCase;
    private final SyncSubareaUseCase syncSubareaUseCase;
    private final SyncMonsterUseCase syncMonsterUseCase;

    public SyncDofus3DataUseCase(
            ReportFileGenerator reportFileGenerator,
            MailSenderService mailSenderService,
            SyncItemTypesUseCase syncItemTypesUseCase,
            SyncItemUseCase syncItemDataUseCase,
            Dofus3LanguageDataProvider languageDataProvider,
            SyncAlmanaxUseCase syncAlmanaxUseCase,
            SyncRecipesUseCase syncRecipeUseCase,
            SyncAreaUseCase syncAreaUseCase,
            SyncSubareaUseCase syncSubareaUseCase,
            SyncMonsterUseCase syncMonsterUseCase) {
        this.reportFileGenerator = reportFileGenerator;
        this.mailSenderService = mailSenderService;
        this.syncItemTypesUseCase = syncItemTypesUseCase;
        this.syncItemUseCase = syncItemDataUseCase;
        this.languageDataProvider = languageDataProvider;
        this.syncAlmanaxUseCase = syncAlmanaxUseCase;
        this.syncRecipeUseCase = syncRecipeUseCase;
        this.syncAreaUseCase = syncAreaUseCase;
        this.syncSubareaUseCase = syncSubareaUseCase;
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

        languageDataProvider.reload();

        // =====================================================
        // ITEM TYPES
        // =====================================================
        SyncReport itemTypeReport = syncItemTypesUseCase.execute(gameVersion);

        // =====================================================
        // ITEMS
        // =====================================================
        SyncReport itemReport = syncItemUseCase.execute(gameVersion);

        // =====================================================
        // ALMANAX
        // =====================================================
        SyncReport almanaxReport = syncAlmanaxUseCase.execute(gameVersion);

        // =====================================================
        // RECIPES
        // =====================================================
        SyncReport recipeReport = syncRecipeUseCase.execute(gameVersion);

        // =====================================================
        // AREAS
        // =====================================================
        SyncReport areaReport = syncAreaUseCase.execute(gameVersion);

        // =====================================================
        // SUBAREAS
        // =====================================================
        SyncReport subareaReport = syncSubareaUseCase.execute(gameVersion);

        // =====================================================
        // MONSTERS
        // =====================================================
        SyncReport monsterReport = syncMonsterUseCase.execute(gameVersion);

        // =====================================================
        // FUTUR : AUTRES JSON
        // XxxSyncReport xxxReport = syncXxxDataUseCase.execute(...);
        // =====================================================

        List<Attachment> attachments = new ArrayList<>();

        StringBuilder body = new StringBuilder();
        body.append("[DOFUS3][SYNC] Rapport de synchronisation\n\n");
        body.append("Date              : ").append(LocalDateTime.now().format(TS)).append('\n');
        body.append("GameVersionId     : ").append(gameVersion.getId()).append('\n');
        body.append("GameVersionCode   : ").append(gameVersion.getCode()).append("\n\n");

        int globalCreated = 0;
        int globalUpdated = 0;

        // ------------- ITEM TYPES -------------
        appendReportSection(
            itemTypeReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += itemTypeReport.createdCount();
        globalUpdated += itemTypeReport.updatedCount();

        // ------------- ITEMS -------------
        appendReportSection(
            itemReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += itemReport.createdCount();
        globalUpdated += itemReport.updatedCount();
        // ------------- ALMANAX -------------
        appendReportSection(
            almanaxReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += almanaxReport.createdCount();
        globalUpdated += almanaxReport.updatedCount();

        // ------------- RECIPES -------------
        appendReportSection(
            recipeReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += recipeReport.createdCount();
        globalUpdated += recipeReport.updatedCount();

        // ------------- AREAS -------------
        appendReportSection(
            areaReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += areaReport.createdCount();
        globalUpdated += areaReport.updatedCount();

        // ------------- SUBAREAS -------------
        appendReportSection(
            subareaReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += subareaReport.createdCount();
        globalUpdated += subareaReport.updatedCount();

        // ------------- MONSTERS -------------
        appendReportSection(
            monsterReport,
            gameVersion,
            body,
            attachments
        );
        globalCreated += monsterReport.createdCount();
        globalUpdated += monsterReport.updatedCount();

        // ------------- TOTAL -------------
        int total = globalCreated + globalUpdated;

        logger.info("[DOFUS3][SYNC] created={}, updated={}", globalCreated, globalUpdated);

        String subject = (total == 0)
                ? "[DOFUS3][SYNC][OK] Aucun changement"
                : "[DOFUS3][SYNC] +" + globalCreated + " ~" + globalUpdated;

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
            String filename = buildAttachmentFilename(gameVersion, report.code());  // ✅
            Path file = reportFileGenerator.generate(
                filename,
                buildAttachmentHeader(gameVersion, report) + "\n" + report.toAttachmentContent()  // ✅
            );
            attachments.add(new Attachment(filename, file));
            body.append("Détails       : voir pièce jointe \"")
                .append(filename)
                .append("\"\n\n");
        }
    }


    private String buildAttachmentFilename(GameVersionData gameVersion, String reportCode) {
        // 1 fichier par JSON traité
        return "dofus3_sync_" + reportCode + "_gv" + gameVersion.getId() + ".txt";
    }

    private String buildAttachmentHeader(GameVersionData gameVersion, SyncReport report) {
        StringBuilder sb = new StringBuilder();
        sb.append("[DOFUS3][SYNC] Détails : ").append(report.label()).append("\n\n");
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
