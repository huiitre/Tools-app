package fr.huiitre.tools.modules.palworld.sync.application.usecase;

import java.util.Map;
import java.util.Optional;

import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import fr.huiitre.tools.modules.core.module.domain.ModuleCode;
import fr.huiitre.tools.modules.core.role.domain.RoleCode;
import fr.huiitre.tools.modules.core.security.application.usecase.SecuredUseCase;
import fr.huiitre.tools.modules.palworld.sync.application.BreedingExceptionSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.ElementSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.MerchantSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldGlobalSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.PalworldSyncReport;
import fr.huiitre.tools.modules.palworld.sync.application.SkillSyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.WorkPrioritySyncResult;
import fr.huiitre.tools.modules.palworld.sync.application.WorkSuitabilitySyncResult;

@Service
@Transactional
public class SyncPalworldUseCase implements SecuredUseCase {

    private final SyncElementsUseCase syncElementsUseCase;
    private final SyncWorkSuitabilitiesUseCase syncWorkSuitabilitiesUseCase;
    private final SyncWorkPrioritiesUseCase syncWorkPrioritiesUseCase;
    private final SyncSkillsUseCase syncSkillsUseCase;
    private final SyncPassiveSkillsUseCase syncPassiveSkillsUseCase;
    private final SyncPalsUseCase syncPalsUseCase;
    private final SyncBreedingExceptionsUseCase syncBreedingExceptionsUseCase;
    private final SyncItemsUseCase syncItemsUseCase;
    private final SyncMerchantsUseCase syncMerchantsUseCase;

    public SyncPalworldUseCase(
            SyncElementsUseCase syncElementsUseCase,
            SyncWorkSuitabilitiesUseCase syncWorkSuitabilitiesUseCase,
            SyncWorkPrioritiesUseCase syncWorkPrioritiesUseCase,
            SyncSkillsUseCase syncSkillsUseCase,
            SyncPassiveSkillsUseCase syncPassiveSkillsUseCase,
            SyncPalsUseCase syncPalsUseCase,
            SyncBreedingExceptionsUseCase syncBreedingExceptionsUseCase,
            SyncItemsUseCase syncItemsUseCase,
            SyncMerchantsUseCase syncMerchantsUseCase) {
        this.syncElementsUseCase = syncElementsUseCase;
        this.syncWorkSuitabilitiesUseCase = syncWorkSuitabilitiesUseCase;
        this.syncWorkPrioritiesUseCase = syncWorkPrioritiesUseCase;
        this.syncSkillsUseCase = syncSkillsUseCase;
        this.syncPassiveSkillsUseCase = syncPassiveSkillsUseCase;
        this.syncPalsUseCase = syncPalsUseCase;
        this.syncBreedingExceptionsUseCase = syncBreedingExceptionsUseCase;
        this.syncItemsUseCase = syncItemsUseCase;
        this.syncMerchantsUseCase = syncMerchantsUseCase;
    }

    @Override
    public Optional<ModuleCode> requiredModule() {
        return Optional.of(ModuleCode.PALWORLD);
    }

    @Override
    public RoleCode requiredRole() {
        return RoleCode.TECH;
    }

    public PalworldGlobalSyncReport execute() {
        // Catalogue complet en tout premier : les drops de Pals (syncPalsUseCase, plus bas) et les offres
        // marchands (syncMerchantsUseCase, plus bas) résolvent leurs FK contre ce catalogue déjà peuplé,
        // ils ne créent/dégradent plus jamais un item eux-mêmes.
        Map<String, Long> itemIdBySlugUpper = syncItemsUseCase.execute();

        ElementSyncResult elements = syncElementsUseCase.execute();
        WorkSuitabilitySyncResult workSuitabilities = syncWorkSuitabilitiesUseCase.execute();
        WorkPrioritySyncResult workPriorities = syncWorkPrioritiesUseCase.execute(workSuitabilities.idBySlug());
        // idByPalElementType : pal.elementTypes[] et skill.element utilisent tous les deux le vocabulaire
        // brut EPalElementType (ex: "Earth"), différent de element.code ("Ground") et de element.name
        // (texte traduit) — cf. elements.json[].palElementType.
        SkillSyncResult skillsCreateOrUpdate = syncSkillsUseCase.syncCreateOrUpdate(elements.idByPalElementType());
        PalworldSyncReport passiveSkills = syncPassiveSkillsUseCase.execute();
        PalworldSyncReport pals = syncPalsUseCase.execute(
                elements.idByPalElementType(),
                workSuitabilities.idBySlug(),
                skillsCreateOrUpdate.idBySlug());
        // Suppression des compétences obsolètes APRÈS la sync des Pals (deleteAllChildren() a déjà vidé
        // pal_active_skill) — sinon FK violation si une compétence supprimée est encore référencée par un
        // Pal de l'ancien run. Voir SyncSkillsUseCase.deleteStale().
        int skillsDeleted = syncSkillsUseCase.deleteStale();
        SkillSyncResult skills = new SkillSyncResult(
                new PalworldSyncReport(skillsCreateOrUpdate.report().created(), skillsCreateOrUpdate.report().updated(), skillsDeleted),
                skillsCreateOrUpdate.idBySlug(),
                skillsCreateOrUpdate.idByName());

        // Résout les tribes de breeding.json en pal.id : doit tourner après syncPalsUseCase (pal.id à jour
        // pour les nouvelles espèces).
        BreedingExceptionSyncReport breedingExceptions = syncBreedingExceptionsUseCase.execute();

        MerchantSyncReport merchants = syncMerchantsUseCase.execute(itemIdBySlugUpper);

        return new PalworldGlobalSyncReport(
                elements.report(), workSuitabilities.report(), workPriorities.report(), skills.report(), passiveSkills, pals,
                breedingExceptions, merchants);
    }
}
